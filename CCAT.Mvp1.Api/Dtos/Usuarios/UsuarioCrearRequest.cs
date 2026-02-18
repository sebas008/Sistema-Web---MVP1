namespace CCAT.Mvp1.Api.DTOs.Usuarios;

public class UsuarioCrearRequest
{
    public string Username { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? Email { get; set; }

    public string Password { get; set; } = "";

    // opcional: asignar rol al crear (ADMIN/USER)
    public string? RolNombre { get; set; }

    public bool Activo { get; set; } = true;

    // auditoría MVP
    public string Usuario { get; set; } = "admin";
}
