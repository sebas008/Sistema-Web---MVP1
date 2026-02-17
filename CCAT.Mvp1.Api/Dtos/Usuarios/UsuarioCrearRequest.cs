namespace CCAT.Mvp1.Api.DTOs.Usuarios;

public class UsuarioCrearRequest
{
    public string Username { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Email { get; set; }
    public string Password { get; set; } = "";
    public string? RolNombre { get; set; }
}
