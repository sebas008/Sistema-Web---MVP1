namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;

public class CompraResponse
{
    public int IdCompra { get; set; }
    public string Numero { get; set; } = "";   // ej: COMP-0001
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";   // REGISTRADA / ANULADA
    public string? Proveedor { get; set; }
}