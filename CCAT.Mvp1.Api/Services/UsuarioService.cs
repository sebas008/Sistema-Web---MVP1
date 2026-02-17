using CCAT.Mvp1.Api.DTOs.Auth;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Interfaces;
using CCAT.Mvp1.Api.Security;

namespace CCAT.Mvp1.Api.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repo;
    public UsuarioService(IUsuarioRepository repo) => _repo = repo;

    public async Task<UsuarioResponse> CrearAsync(UsuarioCrearRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username)) throw new ArgumentException("Username es obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Nombres)) throw new ArgumentException("Nombres es obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Apellidos)) throw new ArgumentException("Apellidos es obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Password)) throw new ArgumentException("Password es obligatorio.");

        var (hash, salt) = PasswordHasher.HashPassword(req.Password);

        return await _repo.CrearAsync(
            req.Username.Trim(),
            req.Nombres.Trim(),
            req.Apellidos.Trim(),
            string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            hash, salt,
            string.IsNullOrWhiteSpace(req.RolNombre) ? null : req.RolNombre.Trim()
        );
    }

    public Task<UsuarioResponse?> ObtenerPorIdAsync(int usuarioId)
        => _repo.ObtenerPorIdAsync(usuarioId);

    public async Task<UsuarioResponse?> ObtenerPorUsernamePublicAsync(string username)
    {
        var u = await _repo.ObtenerPorUsernameAsync(username);
        if (u is null) return null;

        return new UsuarioResponse
        {
            UsuarioId = u.UsuarioId,
            Username = u.Username,
            Nombres = u.Nombres,
            Apellidos = u.Apellidos,
            Email = u.Email,
            Activo = u.Activo,
            CreadoEn = u.CreadoEn
        };
    }

    public Task<List<UsuarioResponse>> ListarAsync(bool? activo, string? q)
        => _repo.ListarAsync(activo, q);

    public Task<UsuarioResponse> ActualizarAsync(int usuarioId, UsuarioActualizarRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombres)) throw new ArgumentException("Nombres es obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Apellidos)) throw new ArgumentException("Apellidos es obligatorio.");
        return _repo.ActualizarAsync(usuarioId, req);
    }

    public async Task CambiarPasswordAsync(int usuarioId, UsuarioCambiarPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword)) throw new ArgumentException("NewPassword es obligatorio.");
        var (hash, salt) = PasswordHasher.HashPassword(req.NewPassword);
        await _repo.CambiarPasswordAsync(usuarioId, hash, salt);
    }

    public Task<UsuarioResponse> CambiarEstadoAsync(int usuarioId, UsuarioCambiarEstadoRequest req)
        => _repo.CambiarEstadoAsync(usuarioId, req.Activo);

    public Task AsignarRolAsync(int usuarioId, UsuarioAsignarRolRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RolNombre)) throw new ArgumentException("RolNombre es obligatorio.");
        return _repo.AsignarRolAsync(usuarioId, req.RolNombre.Trim());
    }

    public async Task<LoginResponse> LoginAsync(LoginDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Username)) throw new ArgumentException("Username es obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Password)) throw new ArgumentException("Password es obligatorio.");

        var u = await _repo.ObtenerPorUsernameAsync(req.Username.Trim());
        if (u is null) throw new ArgumentException("Credenciales inválidas.");
        if (!u.Activo) throw new ArgumentException("Usuario inactivo.");

        var ok = PasswordHasher.VerifyPassword(req.Password, u.PasswordSalt, u.PasswordHash);
        if (!ok) throw new ArgumentException("Credenciales inválidas.");

        var roles = await _repo.ListarRolesAsync(u.UsuarioId);

        return new LoginResponse
        {
            UsuarioId = u.UsuarioId,
            Username = u.Username,
            Nombres = u.Nombres,
            Apellidos = u.Apellidos,
            Email = u.Email,
            Activo = u.Activo,
            Roles = roles.Select(r => r.Nombre).ToList()
        };
    }

}
