using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Entities;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _factory;

    public UsuarioRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    private static object DbOrNull(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : v;

    private static byte[] NewSalt16()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static byte[] CalcHashSha256(byte[] salt16, string passwordPlain)
    {
        var pwdBytes = Encoding.Unicode.GetBytes(passwordPlain ?? "");
        var data = new byte[salt16.Length + pwdBytes.Length];
        Buffer.BlockCopy(salt16, 0, data, 0, salt16.Length);
        Buffer.BlockCopy(pwdBytes, 0, data, salt16.Length, pwdBytes.Length);
        return SHA256.HashData(data);
    }

    public async Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sqlUsers = @"
SELECT TOP (500)
    u.IdUsuario,
    u.Username,
    u.Email,
    u.Nombres,
    u.Apellidos,
    u.Activo
FROM seguridad.Usuario u
WHERE (@activo IS NULL OR u.Activo = @activo)
  AND (
        @q IS NULL
        OR u.Username LIKE '%' + @q + '%'
        OR u.Nombres LIKE '%' + @q + '%'
        OR u.Apellidos LIKE '%' + @q + '%'
        OR u.Email LIKE '%' + @q + '%'
      )
ORDER BY u.IdUsuario DESC;";

        await using var cmd = new SqlCommand(sqlUsers, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@activo", (object?)activo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@q", DbOrNull(q));

        var list = new List<UsuarioResponse>();
        var ids = new List<int>();

        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            while (await rd.ReadAsync())
            {
                var id = rd.GetInt32(rd.GetOrdinal("IdUsuario"));
                ids.Add(id);

                list.Add(new UsuarioResponse
                {
                    IdUsuario = id,
                    Username = rd.GetString(rd.GetOrdinal("Username")),
                    Email = rd.IsDBNull(rd.GetOrdinal("Email")) ? null : rd.GetString(rd.GetOrdinal("Email")),
                    Nombres = rd.IsDBNull(rd.GetOrdinal("Nombres")) ? null : rd.GetString(rd.GetOrdinal("Nombres")),
                    Apellidos = rd.IsDBNull(rd.GetOrdinal("Apellidos")) ? null : rd.GetString(rd.GetOrdinal("Apellidos")),
                    Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
                    RolNombre = null,
                    Roles = new List<Rol>()
                });
            }
        }

        if (ids.Count == 0) return list;

        var idsIn = string.Join(",", ids.Distinct());
        var sqlRoles = $@"
SELECT ur.IdUsuario, r.IdRol, r.Nombre
FROM seguridad.UsuarioRol ur
JOIN seguridad.Rol r ON r.IdRol = ur.IdRol
WHERE ur.IdUsuario IN ({idsIn})
ORDER BY ur.IdUsuario, r.Nombre;";

        var rolesByUser = new Dictionary<int, List<Rol>>();

        await using (var cmdR = new SqlCommand(sqlRoles, (SqlConnection)cn))
        await using (var rdR = await cmdR.ExecuteReaderAsync())
        {
            while (await rdR.ReadAsync())
            {
                var idUsuario = rdR.GetInt32(rdR.GetOrdinal("IdUsuario"));
                var rol = new Rol
                {
                    IdRol = rdR.GetInt32(rdR.GetOrdinal("IdRol")),
                    Nombre = rdR.GetString(rdR.GetOrdinal("Nombre"))
                };

                if (!rolesByUser.TryGetValue(idUsuario, out var arr))
                {
                    arr = new List<Rol>();
                    rolesByUser[idUsuario] = arr;
                }

                arr.Add(rol);
            }
        }

        foreach (var u in list)
        {
            if (rolesByUser.TryGetValue(u.IdUsuario, out var arr))
            {
                u.Roles = arr;
                u.RolNombre = string.Join(", ", arr.Select(x => x.Nombre).Distinct());
            }
        }

        return list;
    }

    public async Task<UsuarioResponse?> ObtenerPorIdAsync(int usuarioId)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT u.IdUsuario, u.Username, u.Email, u.Nombres, u.Apellidos, u.Activo
FROM seguridad.Usuario u
WHERE u.IdUsuario = @id;

SELECT r.IdRol, r.Nombre
FROM seguridad.UsuarioRol ur
JOIN seguridad.Rol r ON r.IdRol = ur.IdRol
WHERE ur.IdUsuario = @id
ORDER BY r.Nombre;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", usuarioId);

        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        var u = new UsuarioResponse
        {
            IdUsuario = rd.GetInt32(rd.GetOrdinal("IdUsuario")),
            Username = rd.GetString(rd.GetOrdinal("Username")),
            Email = rd.IsDBNull(rd.GetOrdinal("Email")) ? null : rd.GetString(rd.GetOrdinal("Email")),
            Nombres = rd.IsDBNull(rd.GetOrdinal("Nombres")) ? null : rd.GetString(rd.GetOrdinal("Nombres")),
            Apellidos = rd.IsDBNull(rd.GetOrdinal("Apellidos")) ? null : rd.GetString(rd.GetOrdinal("Apellidos")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
            Roles = new List<Rol>()
        };

        if (await rd.NextResultAsync())
        {
            while (await rd.ReadAsync())
            {
                u.Roles.Add(new Rol
                {
                    IdRol = rd.GetInt32(rd.GetOrdinal("IdRol")),
                    Nombre = rd.GetString(rd.GetOrdinal("Nombre"))
                });
            }
        }

        u.RolNombre = u.Roles.Count == 0 ? null : string.Join(", ", u.Roles.Select(r => r.Nombre));
        return u;
    }

    public async Task<int> CrearAsync(UsuarioCrearRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        await using (var chk = new SqlCommand(@"
SELECT 
    SUM(CASE WHEN Username = @username THEN 1 ELSE 0 END) AS ExisteUsername,
    SUM(CASE WHEN ISNULL(Email,'') = ISNULL(@email,'') AND @email IS NOT NULL THEN 1 ELSE 0 END) AS ExisteEmail
FROM seguridad.Usuario;", (SqlConnection)cn))
        {
            chk.Parameters.AddWithValue("@username", req.Username);
            chk.Parameters.AddWithValue("@email", DbOrNull(req.Email));

            await using var rd = await chk.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                var existeUsername = !rd.IsDBNull(0) && Convert.ToInt32(rd.GetValue(0)) > 0;
                var existeEmail = !rd.IsDBNull(1) && Convert.ToInt32(rd.GetValue(1)) > 0;

                if (existeUsername)
                    throw new InvalidOperationException($"Ya existe un usuario con el username '{req.Username}'.");

                if (existeEmail)
                    throw new InvalidOperationException($"Ya existe un usuario con el correo '{req.Email}'.");
            }
        }

        var salt = NewSalt16();
        var hash = CalcHashSha256(salt, req.Password);

        var sql = @"
INSERT INTO seguridad.Usuario
(
    Username,
    Email,
    Nombres,
    Apellidos,
    PasswordSalt,
    PasswordHash,
    Activo,
    FechaCreacion,
    UsuarioCreacion
)
VALUES
(
    @username,
    @email,
    @nombres,
    @apellidos,
    @salt,
    @hash,
    @activo,
    SYSDATETIME(),
    @usuario
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@username", req.Username);
        cmd.Parameters.Add("@salt", SqlDbType.VarBinary, 16).Value = salt;
        cmd.Parameters.Add("@hash", SqlDbType.VarBinary, 32).Value = hash;
        cmd.Parameters.AddWithValue("@email", DbOrNull(req.Email));
        cmd.Parameters.AddWithValue("@nombres", DbOrNull(req.Nombres));
        cmd.Parameters.AddWithValue("@apellidos", DbOrNull(req.Apellidos));
        cmd.Parameters.AddWithValue("@activo", req.Activo);
        cmd.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");

        var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return newId;
    }

    public async Task<int> ActualizarAsync(int usuarioId, UsuarioActualizarRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        await using (var chk = new SqlCommand(@"
SELECT COUNT(1)
FROM seguridad.Usuario
WHERE IdUsuario <> @id
  AND ISNULL(Email,'') = ISNULL(@email,'')
  AND @email IS NOT NULL;", (SqlConnection)cn))
        {
            chk.Parameters.AddWithValue("@id", usuarioId);
            chk.Parameters.AddWithValue("@email", DbOrNull(req.Email));
            var count = Convert.ToInt32(await chk.ExecuteScalarAsync() ?? 0);
            if (count > 0)
                throw new InvalidOperationException($"Ya existe otro usuario con el correo '{req.Email}'.");
        }

        var sql = @"
UPDATE seguridad.Usuario
SET Email = @email,
    Nombres = @nombres,
    Apellidos = @apellidos,
    Activo = @activo,
    FechaActualizacion = SYSDATETIME(),
    UsuarioActualizacion = @usuario
WHERE IdUsuario = @id;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.Parameters.AddWithValue("@email", DbOrNull(req.Email));
        cmd.Parameters.AddWithValue("@nombres", DbOrNull(req.Nombres));
        cmd.Parameters.AddWithValue("@apellidos", DbOrNull(req.Apellidos));
        cmd.Parameters.AddWithValue("@activo", req.Activo);
        cmd.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");
        await cmd.ExecuteNonQueryAsync();

        return usuarioId;
    }

    public async Task CambiarPasswordAsync(int usuarioId, string newPassword, string usuario)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var salt = NewSalt16();
        var hash = CalcHashSha256(salt, newPassword);

        var sql = @"
UPDATE seguridad.Usuario
SET PasswordSalt = @salt,
    PasswordHash = @hash,
    FechaActualizacion = SYSDATETIME(),
    UsuarioActualizacion = @usuario
WHERE IdUsuario = @id;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.Parameters.Add("@salt", SqlDbType.VarBinary, 16).Value = salt;
        cmd.Parameters.Add("@hash", SqlDbType.VarBinary, 32).Value = hash;
        cmd.Parameters.AddWithValue("@usuario", DbOrNull(usuario) ?? "admin");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CambiarEstadoAsync(int usuarioId, bool activo, string usuario)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
UPDATE seguridad.Usuario
SET Activo = @activo,
    FechaActualizacion = SYSDATETIME(),
    UsuarioActualizacion = @usuario
WHERE IdUsuario = @id;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        cmd.Parameters.AddWithValue("@activo", activo);
        cmd.Parameters.AddWithValue("@usuario", DbOrNull(usuario) ?? "admin");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AsignarRolAsync(int usuarioId, UsuarioAsignarRolRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        const string schema = "seguridad";
        const string table = "UsuarioRol";

        var cols = await GetColumnsAsync((SqlConnection)cn, schema, table);
        if (cols.Count == 0)
            throw new InvalidOperationException("No existe la tabla seguridad.UsuarioRol o no se pudieron leer sus columnas.");

        var usuarioIdCol = cols.Contains("IdUsuario") ? "IdUsuario" : cols.Contains("UsuarioId") ? "UsuarioId" : null;
        var rolIdCol = cols.Contains("IdRol") ? "IdRol" : cols.Contains("RolId") ? "RolId" : null;

        if (usuarioIdCol is null || rolIdCol is null)
            throw new InvalidOperationException("La tabla seguridad.UsuarioRol debe tener columnas IdUsuario/UsuarioId e IdRol/RolId.");

        var tokens = (req.CsvRoles ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rolMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using (var cmdRoles = new SqlCommand("SELECT IdRol, Nombre FROM seguridad.Rol WHERE Activo = 1;", (SqlConnection)cn))
        await using (var rd = await cmdRoles.ExecuteReaderAsync())
        {
            while (await rd.ReadAsync())
            {
                rolMap[rd.GetString(rd.GetOrdinal("Nombre"))] = rd.GetInt32(rd.GetOrdinal("IdRol"));
            }
        }

        var ids = new List<int>();
        foreach (var t in tokens)
        {
            if (int.TryParse(t, out var idRol))
            {
                ids.Add(idRol);
                continue;
            }

            if (rolMap.TryGetValue(t, out var rid))
                ids.Add(rid);
        }

        ids = ids.Distinct().ToList();

        if (cols.Contains("Activo"))
        {
            var setParts = new List<string> { "Activo = 0" };
            if (cols.Contains("FechaActualizacion")) setParts.Add("FechaActualizacion = SYSDATETIME()");
            if (cols.Contains("UsuarioActualizacion")) setParts.Add("UsuarioActualizacion = @usuario");
            if (cols.Contains("UsuarioAsigno")) setParts.Add("UsuarioAsigno = @usuario");

            var sqlDisable = $@"
UPDATE {schema}.{table}
SET {string.Join(", ", setParts)}
WHERE {usuarioIdCol} = @id AND ISNULL(Activo, 1) = 1;";

            await using var cmdDisable = new SqlCommand(sqlDisable, (SqlConnection)cn);
            cmdDisable.Parameters.AddWithValue("@id", usuarioId);
            cmdDisable.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");
            await cmdDisable.ExecuteNonQueryAsync();
        }
        else
        {
            var sqlDelete = $@"DELETE FROM {schema}.{table} WHERE {usuarioIdCol} = @id;";
            await using var cmdDelete = new SqlCommand(sqlDelete, (SqlConnection)cn);
            cmdDelete.Parameters.AddWithValue("@id", usuarioId);
            await cmdDelete.ExecuteNonQueryAsync();
        }

        foreach (var idRol in ids)
        {
            var insertColumns = new List<string> { usuarioIdCol, rolIdCol };
            var insertValues = new List<string> { "@idUsuario", "@idRol" };

            if (cols.Contains("Activo"))
            {
                insertColumns.Add("Activo");
                insertValues.Add("1");
            }

            if (cols.Contains("FechaCreacion"))
            {
                insertColumns.Add("FechaCreacion");
                insertValues.Add("SYSDATETIME()");
            }

            if (cols.Contains("UsuarioCreacion"))
            {
                insertColumns.Add("UsuarioCreacion");
                insertValues.Add("@usuario");
            }

            if (cols.Contains("FechaActualizacion"))
            {
                insertColumns.Add("FechaActualizacion");
                insertValues.Add("SYSDATETIME()");
            }

            if (cols.Contains("UsuarioActualizacion"))
            {
                insertColumns.Add("UsuarioActualizacion");
                insertValues.Add("@usuario");
            }

            if (cols.Contains("FechaAsignacion"))
            {
                insertColumns.Add("FechaAsignacion");
                insertValues.Add("SYSDATETIME()");
            }

            if (cols.Contains("UsuarioAsignacion"))
            {
                insertColumns.Add("UsuarioAsignacion");
                insertValues.Add("@usuario");
            }

            if (cols.Contains("UsuarioAsigno"))
            {
                insertColumns.Add("UsuarioAsigno");
                insertValues.Add("@usuario");
            }

            var sqlInsert = $@"
INSERT INTO {schema}.{table} ({string.Join(", ", insertColumns)})
VALUES ({string.Join(", ", insertValues)});";

            await using var cmdInsert = new SqlCommand(sqlInsert, (SqlConnection)cn);
            cmdInsert.Parameters.AddWithValue("@idUsuario", usuarioId);
            cmdInsert.Parameters.AddWithValue("@idRol", idRol);
            cmdInsert.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");
            await cmdInsert.ExecuteNonQueryAsync();
        }
    }

    private static async Task<HashSet<string>> GetColumnsAsync(SqlConnection cn, string schema, string tableName)
    {
        const string sql = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            cols.Add(dr.GetString(0));
        }

        return cols;
    }
}