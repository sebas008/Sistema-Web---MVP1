namespace CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;

public class OrdenServicioDetalleResponse
{
    public int IdOrdenServicioDetalle { get; set; }
    public int IdOrdenServicio { get; set; }
    public int Item { get; set; }
    public string Tipo { get; set; } = "";
    public int? IdProducto { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }
}