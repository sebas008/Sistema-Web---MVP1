namespace CCAT.Mvp1.Api.DTOs.Inventario;

public class StockProductoResponse
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public decimal CantidadActual { get; set; }
    public string? Referencia { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
