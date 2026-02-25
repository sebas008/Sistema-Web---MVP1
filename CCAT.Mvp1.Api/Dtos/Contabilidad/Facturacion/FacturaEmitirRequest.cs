using CCAT.Mvp1.Api.DTOs.Contabilidad;

namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;

public class FacturaEmitirRequest
{
    public string Serie { get; set; } = "F001";
    public int IdCliente { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public string Moneda { get; set; } = "PEN";
    public bool AfectaStock { get; set; } = true;

    public List<DetalleItemDto> Detalle { get; set; } = new();

    public string Usuario { get; set; } = "admin";
}
