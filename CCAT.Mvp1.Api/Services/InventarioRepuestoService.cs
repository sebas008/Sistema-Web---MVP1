using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class InventarioRepuestoService : IInventarioRepuestoService
{
    private readonly IInventarioRepuestoRepository _repo;

    public InventarioRepuestoService(IInventarioRepuestoRepository repo)
    {
        _repo = repo;
    }

    public Task<List<StockProductoResponse>> ListarStockAsync(string? q)
        => _repo.ListarStockAsync(q);

    public Task<StockProductoResponse?> ObtenerStockAsync(int idProducto)
    {
        if (idProducto <= 0) throw new ArgumentException("IdProducto inválido.");
        return _repo.ObtenerStockAsync(idProducto);
    }

    public Task AplicarMovimientoAsync(StockProductoMovimientoRequest req)
    {
        if (req.IdProducto <= 0) throw new ArgumentException("IdProducto inválido.");
        if (req.Cantidad <= 0) throw new ArgumentException("Cantidad debe ser > 0.");
        if (string.IsNullOrWhiteSpace(req.TipoMovimiento)) throw new ArgumentException("TipoMovimiento obligatorio.");

        req.TipoMovimiento = req.TipoMovimiento.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(req.Usuario)) req.Usuario = "admin";
        if (string.IsNullOrWhiteSpace(req.Referencia)) req.Referencia = "API";

        return _repo.AplicarMovimientoAsync(req);
    }
}
