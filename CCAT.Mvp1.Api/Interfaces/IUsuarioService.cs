using CCAT.Mvp1.Api.DTOs.Auth;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using Microsoft.AspNetCore.Identity.Data;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IUsuarioService
{
    // CREATE
    Task<UsuarioResponse> CrearAsync(UsuarioCrearRequest req);

    // READ
    Task<UsuarioResponse?> ObtenerPorIdAsync(int usuarioId);
    Task<UsuarioResponse?> ObtenerPorUsernamePublicAsync(string username);
    Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q);

    // UPDATE
    Task<UsuarioResponse> ActualizarAsync(int usuarioId, UsuarioActualizarRequest req);
    Task CambiarPasswordAsync(int usuarioId, UsuarioCambiarPasswordRequest req);
    Task<UsuarioResponse> CambiarEstadoAsync(int usuarioId, UsuarioCambiarEstadoRequest req);

    // ROLES
    Task AsignarRolAsync(int usuarioId, UsuarioAsignarRolRequest req);

    // AUTH
    Task<LoginResponse> LoginAsync(LoginDto req);
}
