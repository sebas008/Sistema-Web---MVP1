namespace CCAT.Mvp1.Api.DTOs.VehiculosNuevos;

public class VehiculoNuevoResponse
{
    public int IdVehiculo { get; set; }
    public string? VIN { get; set; }
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public string Version { get; set; } = "";
    public string Color { get; set; } = "";
    public decimal PrecioLista { get; set; }
    public bool Activo { get; set; }
    public decimal StockActual { get; set; } // si tu SP lo devuelve (recomendado)
}
