namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;

public class CompraRegistrarRequest
{
    public int IdProveedor { get; set; }
    public DateTime? Fecha { get; set; }
    public string Usuario { get; set; } = "admin";

    // MVP: si tu SP recibe detalle como JSON/CSV, lo agregas aquí
     public string? DetalleJson { get; set; }
}