namespace CCAT.Mvp1.Api.Entities;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = "";
    public string? Email { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public bool Activo { get; set; }

    // Interno (login)
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
}
