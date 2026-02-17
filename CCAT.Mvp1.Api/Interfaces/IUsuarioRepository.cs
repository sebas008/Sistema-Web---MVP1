using CCAT.Mvp1.Api.DTOs.Auth;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Entities;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IUsuarioRepository
{
    // CREATE
    Task<UsuarioResponse> CrearAsync(string username, string nombres, string apellidos, string? email,
        byte[] passwordHash, byte[] passwordSalt, string? rolNombre);

    // READ
    Task<Usuario?> ObtenerPorUsernameAsync(string username);          // interno (incluye hash/salt)
    Task<UsuarioResponse?> ObtenerPorIdAsync(int usuarioId);          // público
    Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q); // público

    // UPDATE
    Task<UsuarioResponse> ActualizarAsync(int usuarioId, UsuarioActualizarRequest req);
    Task CambiarPasswordAsync(int usuarioId, byte[] hash, byte[] salt);
    Task<UsuarioResponse> CambiarEstadoAsync(int usuarioId, bool activo);

    // ROLES
    Task AsignarRolAsync(int usuarioId, string rolNombre);
    Task<List<Rol>> ListarRolesAsync(int usuarioId);
}
