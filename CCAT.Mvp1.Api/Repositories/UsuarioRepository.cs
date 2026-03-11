using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Entities;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

// Implementación alineada a SISTEMA_CCAT.sql (sin SPs de usuario/rol)
public class UsuarioRepository : IUsuarioRepository
{
    private static byte[] NewSalt16()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    // Debe replicar: HASHBYTES('SHA2_256', @Salt + CONVERT(VARBINARY(400), @PasswordPlain))
    private static byte[] CalcHashSha256(byte[] salt16, string passwordPlain)
    {
        var pwdBytes = Encoding.Unicode.GetBytes(passwordPlain ?? ""); // NVARCHAR -> UCS-2 LE
        var data = new byte[salt16.Length + pwdBytes.Length];
        Buffer.BlockCopy(salt16, 0, data, 0, salt16.Length);
        Buffer.BlockCopy(pwdBytes, 0, data, salt16.Length, pwdBytes.Length);
        return SHA256.HashData(data); // 32 bytes
    }

    private readonly IDbConnectionFactory _factory;
    public UsuarioRepository(IDbConnectionFactory factory) => _factory = factory;

    private static object DbOrNull(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : v;

    public async Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q)
{
    await using var cn = _factory.CreateConnection();
    await cn.OpenAsync();

    // IMPORTANTE: evitamos STRING_AGG por compatibilidad (SQL Server < 2017).
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

    var rolesByUser = new Dictionary<int, List<string>>();
    await using (var cmdR = new SqlCommand(sqlRoles, (SqlConnection)cn))
    await using (var rdR = await cmdR.ExecuteReaderAsync())
    {
        while (await rdR.ReadAsync())
        {
            var idUsuario = rdR.GetInt32(rdR.GetOrdinal("IdUsuario"));
            var nombre = rdR.GetString(rdR.GetOrdinal("Nombre"));

            if (!rolesByUser.TryGetValue(idUsuario, out var arr))
            {
                arr = new List<string>();
                rolesByUser[idUsuario] = arr;
            }
            arr.Add(nombre);
        }
    }

    foreach (var u in list)
    {
        if (rolesByUser.TryGetValue(u.IdUsuario, out var arr))
        {
            u.RolNombre = string.Join(", ", arr.Distinct());
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

        // SISTEMA_CCAT.sql usa PasswordSalt (16) + PasswordHash (32): SHA2_256(salt + varbinary(passwordPlain))
        var salt = NewSalt16();
        var hash = CalcHashSha256(salt, req.Password);

        var sql = @"
INSERT INTO seguridad.Usuario (Username, Email, Nombres, Apellidos, PasswordSalt, PasswordHash, Activo, FechaCreacion, UsuarioCreacion)
VALUES (@username, @email, @nombres, @apellidos, @salt, @hash, @activo, SYSDATETIME(), @usuario);
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

        var sql = @"
UPDATE seguridad.Usuario
SET Email=@email,
    Nombres=@nombres,
    Apellidos=@apellidos,
    Activo=@activo,
    FechaActualizacion=SYSDATETIME(),
    UsuarioActualizacion=@usuario
WHERE IdUsuario=@id;";

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
SET PasswordSalt=@salt,
    PasswordHash=@hash,
    FechaActualizacion=SYSDATETIME(),
    UsuarioActualizacion=@usuario
WHERE IdUsuario=@id;";

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
SET Activo=@activo,
    FechaActualizacion=SYSDATETIME(),
    UsuarioActualizacion=@usuario
WHERE IdUsuario=@id;";

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

        // csvRoles:
        // - El FRONT suele enviar IDs ("1,2")
        // - También soportamos nombres ("ADMIN,USER")
        var tokens = (req.CsvRoles ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // buscamos ids por nombre (case-insensitive)
        var getIdsSql = @"SELECT IdRol, Nombre FROM seguridad.Rol WHERE Activo=1;";
        var rolMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using (var cmdRoles = new SqlCommand(getIdsSql, (SqlConnection)cn))
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
            {
                ids.Add(rid);
            }
        }
        ids = ids.Distinct().ToList();

        // Reemplazamos roles
        await using (var del = new SqlCommand("DELETE FROM seguridad.UsuarioRol WHERE IdUsuario=@id;", (SqlConnection)cn))
        {
            del.Parameters.AddWithValue("@id", usuarioId);
            await del.ExecuteNonQueryAsync();
        }

        foreach (var idRol in ids)
        {
            var ins = @"
IF COL_LENGTH('seguridad.UsuarioRol','FechaAsignacion') IS NOT NULL
BEGIN
  INSERT INTO seguridad.UsuarioRol (IdUsuario, IdRol, FechaAsignacion, UsuarioAsignacion)
  VALUES (@idUsuario, @idRol, SYSDATETIME(), @usuario);
END
ELSE
BEGIN
  INSERT INTO seguridad.UsuarioRol (IdUsuario, IdRol, FechaCreacion, UsuarioCreacion)
  VALUES (@idUsuario, @idRol, SYSDATETIME(), @usuario);
END
";
            await using var cmd = new SqlCommand(ins, (SqlConnection)cn);
            cmd.Parameters.AddWithValue("@idUsuario", usuarioId);
            cmd.Parameters.AddWithValue("@idRol", idRol);
            cmd.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
