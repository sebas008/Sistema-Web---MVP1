namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;

public class FacturaDetalleItemResponse
{
    public int Item { get; set; }
    public string TipoItem { get; set; } = ""; // PRODUCTO / SERVICIO / VEHICULO
    public int? IdProducto { get; set; }
    public int? IdVehiculo { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }
}

public class FacturaResponse
{
    public int IdFactura { get; set; }
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "";
    public string? Cliente { get; set; }

    // Detalle (solo se llena en Obtener por Id)
    public List<FacturaDetalleItemResponse>? Detalle { get; set; }
}
