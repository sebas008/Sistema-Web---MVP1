namespace CCAT.Mvp1.Api.DTOs.Inventario;

public class ProductoCrearRequest
{
    public string Codigo { get; set; } = "";     // SKU / código único
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
}
