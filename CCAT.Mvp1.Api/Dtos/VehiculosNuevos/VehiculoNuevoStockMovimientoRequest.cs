namespace CCAT.Mvp1.Api.DTOs.VehiculosNuevos;

public class VehiculoNuevoStockMovimientoRequest
{
    public int IdVehiculo { get; set; }
    public decimal Cantidad { get; set; }            // > 0
    public string TipoMovimiento { get; set; } = ""; // ENTRADA | SALIDA | AJUSTE
    public string? Referencia { get; set; }          // "COMPRA", "VENTA", etc.
    public string Usuario { get; set; } = "admin";
}
