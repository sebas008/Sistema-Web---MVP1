namespace CCAT.Mvp1.Api.DTOs.VehiculosNuevos;

public class VehiculoNuevoCambiarEstadoRequest
{
    public bool Activo { get; set; }
    public string Usuario { get; set; } = "admin";
}
