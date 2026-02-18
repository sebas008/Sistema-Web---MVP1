using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repo;

    public UsuarioService(IUsuarioRepository repo)
    {
        _repo = repo;
    }

    public async Task<UsuarioResponse> CrearAsync(UsuarioCrearRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username)) throw new ArgumentException("Username obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Password)) throw new ArgumentException("Password obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Usuario)) req.Usuario = "admin";

        var id = await _repo.CrearAsync(req);
        var u = await _repo.ObtenerPorIdAsync(id);
        if (u == null) throw new Exception("No se pudo obtener el usuario creado.");
        return u;
    }

    public Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q)
        => _repo.ListarAsync(activo, q);

    public Task<UsuarioResponse?> ObtenerPorIdAsync(int id)
        => _repo.ObtenerPorIdAsync(id);

    public async Task<UsuarioResponse> ActualizarAsync(int id, UsuarioActualizarRequest req)
    {
        await _repo.ActualizarAsync(id, req);
        var u = await _repo.ObtenerPorIdAsync(id);
        if (u == null) throw new Exception("Usuario no encontrado.");
        return u;
    }

    public async Task<UsuarioResponse> CambiarEstadoAsync(int id, UsuarioCambiarEstadoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Usuario)) req.Usuario = "admin";

        await _repo.CambiarEstadoAsync(id, req.Activo, req.Usuario);

        var u = await _repo.ObtenerPorIdAsync(id);
        if (u == null) throw new Exception("Usuario no encontrado.");
        return u;
    }

    public Task CambiarPasswordAsync(int id, UsuarioCambiarPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Usuario)) req.Usuario = "admin";
        return _repo.CambiarPasswordAsync(id, req.NewPassword, req.Usuario);
    }

    public Task AsignarRolAsync(int id, UsuarioAsignarRolRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Usuario)) req.Usuario = "admin";
        return _repo.AsignarRolAsync(id, req);
    }
}
