namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;

public class CompraResponse
{
    public int IdCompra { get; set; }
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
    public string? Proveedor { get; set; }
    public List<CompraDetalleItemDto> Detalle { get; set; } = new();
}
