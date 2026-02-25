using CCAT.Mvp1.Api.DTOs.Contabilidad;

namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;

public class CompraRegistrarRequest
{
    public string Serie { get; set; } = "C001";
    public int IdProveedor { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Today;
    public string Moneda { get; set; } = "PEN";
    public bool AfectaStock { get; set; } = true;

    public List<DetalleItemDto> Detalle { get; set; } = new();

    public string Usuario { get; set; } = "admin";
}
