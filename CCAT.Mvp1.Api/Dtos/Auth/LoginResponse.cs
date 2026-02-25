namespace CCAT.Mvp1.Api.Models;

public class LoginResponse
{
    public int UsuarioId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}