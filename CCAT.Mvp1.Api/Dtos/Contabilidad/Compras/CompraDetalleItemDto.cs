namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;

public class CompraDetalleItemDto
{
    public int? Item { get; set; }
    public int? IdProducto { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal? Importe { get; set; }
}
