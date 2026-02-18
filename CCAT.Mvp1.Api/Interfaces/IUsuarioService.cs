using CCAT.Mvp1.Api.DTOs.Usuarios;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IUsuarioService
{
    Task<UsuarioResponse> CrearAsync(UsuarioCrearRequest req);
    Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q);
    Task<UsuarioResponse?> ObtenerPorIdAsync(int id);

    Task<UsuarioResponse> ActualizarAsync(int id, UsuarioActualizarRequest req);
    Task<UsuarioResponse> CambiarEstadoAsync(int id, UsuarioCambiarEstadoRequest req);
    Task CambiarPasswordAsync(int id, UsuarioCambiarPasswordRequest req);

    Task AsignarRolAsync(int id, UsuarioAsignarRolRequest req);
}
