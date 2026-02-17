namespace CCAT.Mvp1.Api.Entities;

public class Usuario
{
    public int UsuarioId { get; set; }
    public string Username { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Email { get; set; }

    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}
