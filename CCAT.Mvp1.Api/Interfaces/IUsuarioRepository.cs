using CCAT.Mvp1.Api.DTOs.Usuarios;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IUsuarioRepository
{
    Task<int> CrearAsync(UsuarioCrearRequest req);
    Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q);
    Task<UsuarioResponse?> ObtenerPorIdAsync(int id);
    Task<int> ActualizarAsync(int id, UsuarioActualizarRequest req);
    Task CambiarEstadoAsync(int id, bool activo, string usuario);
    Task CambiarPasswordAsync(int id, string newPassword, string usuario);

    Task AsignarRolAsync(int id, UsuarioAsignarRolRequest req);
}
