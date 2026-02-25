using CCAT.Mvp1.Api.Entities;

namespace CCAT.Mvp1.Api.DTOs.Usuarios;

public class UsuarioResponse
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public bool Activo { get; set; }

    // Para listado (UI): rol principal/csv
    public string? RolNombre { get; set; }

    public List<Rol> Roles { get; set; } = new();
}
