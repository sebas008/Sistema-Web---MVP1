namespace CCAT.Mvp1.Api.DTOs.Inventario;

public class StockProductoMovimientoRequest
{
    public int IdProducto { get; set; }
    public decimal Cantidad { get; set; }              // > 0
    public string TipoMovimiento { get; set; } = "";   // ENTRADA | SALIDA | AJUSTE
    public string? Referencia { get; set; }            // "COMPRA", "VENTA", "INIT", etc.
    public string Usuario { get; set; } = "admin";
}
