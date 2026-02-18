namespace CCAT.Mvp1.Api.DTOs.Usuarios;

public class UsuarioAsignarRolRequest
{
    public string CsvRoles { get; set; } = ""; // ejemplo: "1,2"
    public string Usuario { get; set; } = "admin";
}
