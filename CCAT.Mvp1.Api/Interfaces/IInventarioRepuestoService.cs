using CCAT.Mvp1.Api.DTOs.Inventario;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IInventarioRepuestoService
{
    Task<List<StockProductoResponse>> ListarStockAsync(string? q);
    Task<StockProductoResponse?> ObtenerStockAsync(int idProducto);
    Task AplicarMovimientoAsync(StockProductoMovimientoRequest req);
}
