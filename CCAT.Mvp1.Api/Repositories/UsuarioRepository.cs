using System.Data;
using Dapper;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Entities;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _factory;
    public UsuarioRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<UsuarioResponse> CrearAsync(string username, string nombres, string apellidos, string? email,
        byte[] passwordHash, byte[] passwordSalt, string? rolNombre)
    {
        using var cn = _factory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@Username", username);
        p.Add("@Nombres", nombres);
        p.Add("@Apellidos", apellidos);
        p.Add("@Email", email);
        p.Add("@PasswordHash", passwordHash, DbType.Binary, size: 64);
        p.Add("@PasswordSalt", passwordSalt, DbType.Binary, size: 32);
        p.Add("@RolNombre", rolNombre);

        return await cn.QuerySingleAsync<UsuarioResponse>(
            "seguridad.usp_Usuario_Crear",
            p,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<Usuario?> ObtenerPorUsernameAsync(string username)
    {
        using var cn = _factory.CreateConnection();

        return await cn.QueryFirstOrDefaultAsync<Usuario>(
            "seguridad.usp_Usuario_ObtenerPorUsername",
            new { Username = username },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<UsuarioResponse?> ObtenerPorIdAsync(int usuarioId)
    {
        using var cn = _factory.CreateConnection();

        return await cn.QueryFirstOrDefaultAsync<UsuarioResponse>(
            "seguridad.usp_Usuario_ObtenerPorId",
            new { UsuarioId = usuarioId },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q)
    {
        using var cn = _factory.CreateConnection();

        var rows = await cn.QueryAsync<UsuarioResponse>(
            "seguridad.usp_Usuario_Listar",
            new { Activo = activo, Q = q },
            commandType: CommandType.StoredProcedure
        );

        return rows.ToList();
    }

    public async Task<UsuarioResponse> ActualizarAsync(int usuarioId, UsuarioActualizarRequest req)
    {
        using var cn = _factory.CreateConnection();

        return await cn.QuerySingleAsync<UsuarioResponse>(
            "seguridad.usp_Usuario_Actualizar",
            new { UsuarioId = usuarioId, req.Nombres, req.Apellidos, req.Email },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task CambiarPasswordAsync(int usuarioId, byte[] hash, byte[] salt)
    {
        using var cn = _factory.CreateConnection();

        await cn.ExecuteAsync(
            "seguridad.usp_Usuario_CambiarPassword",
            new { UsuarioId = usuarioId, PasswordHash = hash, PasswordSalt = salt },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<UsuarioResponse> CambiarEstadoAsync(int usuarioId, bool activo)
    {
        using var cn = _factory.CreateConnection();

        return await cn.QuerySingleAsync<UsuarioResponse>(
            "seguridad.usp_Usuario_CambiarEstado",
            new { UsuarioId = usuarioId, Activo = activo },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task AsignarRolAsync(int usuarioId, string rolNombre)
    {
        using var cn = _factory.CreateConnection();

        await cn.ExecuteAsync(
            "seguridad.usp_Usuario_AsignarRol",
            new { UsuarioId = usuarioId, RolNombre = rolNombre },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<List<Rol>> ListarRolesAsync(int usuarioId)
    {
        using var cn = _factory.CreateConnection();

        var rows = await cn.QueryAsync<Rol>(
            "seguridad.usp_Usuario_ListarRoles",
            new { UsuarioId = usuarioId },
            commandType: CommandType.StoredProcedure
        );

        return rows.ToList();
    }
}
