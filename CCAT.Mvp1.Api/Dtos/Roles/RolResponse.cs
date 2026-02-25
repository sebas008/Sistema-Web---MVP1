namespace CCAT.Mvp1.Api.DTOs.Roles;

public class RolResponse
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}
