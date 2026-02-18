namespace CCAT.Mvp1.Api.DTOs.Usuarios;

public class UsuarioCambiarPasswordRequest
{
    public string NewPassword { get; set; } = "";
    public string Usuario { get; set; } = "admin";
}
