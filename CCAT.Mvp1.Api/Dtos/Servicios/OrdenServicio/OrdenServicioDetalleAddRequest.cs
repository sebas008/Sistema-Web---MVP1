namespace CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;

public class OrdenServicioDetalleAddRequest
{
    public string Tipo { get; set; } = "SERVICIO";   // SERVICIO / REPUESTO
    public int? IdProducto { get; set; }             // si REPUESTO
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; } = 0;

    public string Usuario { get; set; } = "admin";
}