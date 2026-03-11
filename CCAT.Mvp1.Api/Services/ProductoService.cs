using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class ProductoService : IProductoService
{
    private readonly IProductoRepository _repo;
    public ProductoService(IProductoRepository repo) => _repo = repo;

    public Task<ProductoResponse> CrearAsync(ProductoCrearRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Codigo)) throw new ArgumentException("Codigo es obligatorio.");
        if (string.IsNullOrWhiteSpace(req.Nombre)) throw new ArgumentException("Nombre es obligatorio.");
        if (req.Precio < 0) throw new ArgumentException("Precio inválido.");
        return _repo.CrearAsync(req);
    }

    public Task<List<ProductoResponse>> ListarAsync(string? q, bool? activo)
        => _repo.ListarAsync(q, activo);

    public Task<ProductoResponse?> ObtenerPorIdAsync(int productoId)
        => _repo.ObtenerPorIdAsync(productoId);

    public Task<ProductoResponse> ActualizarAsync(int productoId, ProductoActualizarRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre)) throw new ArgumentException("Nombre es obligatorio.");
        if (req.Precio < 0) throw new ArgumentException("Precio inválido.");
        return _repo.ActualizarAsync(productoId, req);
    }

    public Task<ProductoResponse> CambiarEstadoAsync(int productoId, bool activo)
        => _repo.CambiarEstadoAsync(productoId, activo);

    public Task EliminarAsync(int productoId)
    {
        if (productoId <= 0) throw new ArgumentException("Id de producto inválido.");
        return _repo.EliminarAsync(productoId);
    }
}
