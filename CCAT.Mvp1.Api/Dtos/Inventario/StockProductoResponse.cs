namespace CCAT.Mvp1.Api.DTOs.Inventario;

public class StockProductoResponse
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    // En BD: TipoProducto (lo usamos como Categoría)
    public string? Categoria { get; set; }
    public string Codigo { get; set; } = "";
    public decimal? Precio { get; set; }
    // Para el front: mostramos como "stock"
    public decimal Stock { get; set; }
    public string? Referencia { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
