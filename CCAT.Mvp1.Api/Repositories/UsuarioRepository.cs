using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Entities;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    public UsuarioRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    // helper para DBNull sin problemas de ?? con string no-nullable
    private static object DbOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    public async Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_Usuario_List", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@SoloActivos", (object?)activo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Q", DbOrNull(q));

        var list = new List<UsuarioResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new UsuarioResponse
            {
                IdUsuario = rd.GetInt32(rd.GetOrdinal("IdUsuario")),
                Username = rd.GetString(rd.GetOrdinal("Username")),
                Email = rd.IsDBNull(rd.GetOrdinal("Email")) ? null : rd.GetString(rd.GetOrdinal("Email")),
                Nombres = rd.IsDBNull(rd.GetOrdinal("Nombres")) ? "" : rd.GetString(rd.GetOrdinal("Nombres")),
                Apellidos = rd.IsDBNull(rd.GetOrdinal("Apellidos")) ? "" : rd.GetString(rd.GetOrdinal("Apellidos")),
                Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
                Roles = new List<Rol>() // en listado no traemos roles
            });
        }

        return list;
    }

    public async Task<UsuarioResponse?> ObtenerPorIdAsync(int usuarioId)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_Usuario_Get", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdUsuario", usuarioId);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        var u = new UsuarioResponse
        {
            IdUsuario = rd.GetInt32(rd.GetOrdinal("IdUsuario")),
            Username = rd.GetString(rd.GetOrdinal("Username")),
            Email = rd.IsDBNull(rd.GetOrdinal("Email")) ? null : rd.GetString(rd.GetOrdinal("Email")),
            Nombres = rd.IsDBNull(rd.GetOrdinal("Nombres")) ? "" : rd.GetString(rd.GetOrdinal("Nombres")),
            Apellidos = rd.IsDBNull(rd.GetOrdinal("Apellidos")) ? "" : rd.GetString(rd.GetOrdinal("Apellidos")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
            Roles = new List<Rol>()
        };

        // 2do resultset: roles (columna "Nombre")
        if (await rd.NextResultAsync())
        {
            while (await rd.ReadAsync())
            {
                u.Roles.Add(new Rol
                {
                    IdRol = 0,
                    Nombre = rd.GetString(rd.GetOrdinal("Nombre"))
                });
            }
        }

        return u;
    }

    public async Task<int> CrearAsync(UsuarioCrearRequest req)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_Usuario_Upsert", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdUsuario", DBNull.Value);
        cmd.Parameters.AddWithValue("@Username", req.Username);
        cmd.Parameters.AddWithValue("@Email", DbOrNull(req.Email));
        cmd.Parameters.AddWithValue("@Nombres", DbOrNull(req.Nombres));
        cmd.Parameters.AddWithValue("@Apellidos", DbOrNull(req.Apellidos));
        cmd.Parameters.AddWithValue("@Activo", req.Activo);
        cmd.Parameters.AddWithValue("@PasswordPlain", req.Password);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<int> ActualizarAsync(int usuarioId, UsuarioActualizarRequest req)
    {
        var actual = await ObtenerPorIdAsync(usuarioId);
        if (actual == null) throw new Exception("Usuario no existe.");

        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_Usuario_Upsert", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdUsuario", usuarioId);
        cmd.Parameters.AddWithValue("@Username", actual.Username);
        cmd.Parameters.AddWithValue("@Email", DbOrNull(req.Email));
        cmd.Parameters.AddWithValue("@Nombres", DbOrNull(req.Nombres));
        cmd.Parameters.AddWithValue("@Apellidos", DbOrNull(req.Apellidos));
        cmd.Parameters.AddWithValue("@Activo", actual.Activo);
        cmd.Parameters.AddWithValue("@PasswordPlain", DBNull.Value);
        cmd.Parameters.AddWithValue("@Usuario", "admin");

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task CambiarPasswordAsync(int usuarioId, string newPassword, string usuario)
    {
        var actual = await ObtenerPorIdAsync(usuarioId);
        if (actual == null) throw new Exception("Usuario no existe.");

        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_Usuario_Upsert", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdUsuario", usuarioId);
        cmd.Parameters.AddWithValue("@Username", actual.Username);
        cmd.Parameters.AddWithValue("@Email", DbOrNull(actual.Email));
        cmd.Parameters.AddWithValue("@Nombres", DbOrNull(actual.Nombres));
        cmd.Parameters.AddWithValue("@Apellidos", DbOrNull(actual.Apellidos));
        cmd.Parameters.AddWithValue("@Activo", actual.Activo);
        cmd.Parameters.AddWithValue("@PasswordPlain", newPassword);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(usuario));

        await cmd.ExecuteScalarAsync();
    }

    public async Task CambiarEstadoAsync(int usuarioId, bool activo, string usuario)
    {
        var actual = await ObtenerPorIdAsync(usuarioId);
        if (actual == null) throw new Exception("Usuario no existe.");

        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_Usuario_Upsert", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdUsuario", usuarioId);
        cmd.Parameters.AddWithValue("@Username", actual.Username);
        cmd.Parameters.AddWithValue("@Email", DbOrNull(actual.Email));
        cmd.Parameters.AddWithValue("@Nombres", DbOrNull(actual.Nombres));
        cmd.Parameters.AddWithValue("@Apellidos", DbOrNull(actual.Apellidos));
        cmd.Parameters.AddWithValue("@Activo", activo);
        cmd.Parameters.AddWithValue("@PasswordPlain", DBNull.Value);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(usuario));

        await cmd.ExecuteScalarAsync();
    }

    // Firma POR DTO para calzar con tu Service/Controller
    public async Task AsignarRolAsync(int usuarioId, UsuarioAsignarRolRequest req)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_UsuarioRol_Set", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdUsuario", usuarioId);
        cmd.Parameters.AddWithValue("@CsvRoles", DbOrNull(req.CsvRoles));
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        await cmd.ExecuteNonQueryAsync();
    }
}
