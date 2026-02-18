namespace CCAT.Mvp1.Api.DTOs.VehiculosNuevos;

public class VehiculoNuevoActualizarRequest
{
    public string? VIN { get; set; }
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public string Version { get; set; } = "";
    public string Color { get; set; } = "";
    public decimal PrecioLista { get; set; }
    public string Usuario { get; set; } = "admin";
}
