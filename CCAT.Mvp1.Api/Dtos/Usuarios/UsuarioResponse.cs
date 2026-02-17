namespace CCAT.Mvp1.Api.DTOs.Usuarios;

public class UsuarioResponse
{
    public int UsuarioId { get; set; }
    public string Username { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Email { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}
