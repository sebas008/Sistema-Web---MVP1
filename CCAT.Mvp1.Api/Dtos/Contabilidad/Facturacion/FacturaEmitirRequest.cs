namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;

public class FacturaEmitirRequest
{
    public int IdCliente { get; set; }

    // opcional si tu SP lo usa
    public DateTime? Fecha { get; set; }

    // quién genera la factura (para trazabilidad)
    public string Usuario { get; set; } = "admin";

    // MVP: si tu SP recibe un JSON o CSV de detalle, lo pones aquí
     public string? DetalleJson { get; set; }
}