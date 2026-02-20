namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;

public class FacturaResponse
{
    public int IdFactura { get; set; }
    public string Numero { get; set; } = "";   // ej: FACT-0001
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";   // EMITIDA / ANULADA
    public string? Cliente { get; set; }
}