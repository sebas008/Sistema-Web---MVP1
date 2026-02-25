namespace CCAT.Mvp1.Api.DTOs.Usuarios;

public class UsuarioActualizarRequest
{
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Email { get; set; }

    // Requerido para que la actualización mantenga estado y trazabilidad.
    public bool Activo { get; set; } = true;
    public string? Usuario { get; set; }
}
