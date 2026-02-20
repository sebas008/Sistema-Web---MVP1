namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;

public class GuiaEmitirRequest
{
    public int IdCliente { get; set; }          // o IdDestino/IdAlmacen según tu lógica
    public DateTime? Fecha { get; set; }
    public string Usuario { get; set; } = "admin";

    // MVP: si tu SP recibe detalle (items) como JSON/CSV, lo agregas aquí
     public string? DetalleJson { get; set; }
}