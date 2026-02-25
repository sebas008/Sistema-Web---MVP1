namespace CCAT.Mvp1.Api.DTOs.Contabilidad;

public class DetalleItemDto
{
    public int? IdProducto { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
