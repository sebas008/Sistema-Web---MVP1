using CCAT.Mvp1.Api.DTOs.Inventario;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IProductoRepository
{
    Task<ProductoResponse> CrearAsync(ProductoCrearRequest req);
    Task<List<ProductoResponse>> ListarAsync(string? q, bool? activo);
    Task<ProductoResponse?> ObtenerPorIdAsync(int productoId);
    Task<ProductoResponse> ActualizarAsync(int productoId, ProductoActualizarRequest req);
    Task<ProductoResponse> CambiarEstadoAsync(int productoId, bool activo);
    Task EliminarAsync(int productoId);
}
