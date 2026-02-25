namespace CCAT.Mvp1.Api.DTOs.Inventario;

public class ProductoActualizarRequest
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
}
