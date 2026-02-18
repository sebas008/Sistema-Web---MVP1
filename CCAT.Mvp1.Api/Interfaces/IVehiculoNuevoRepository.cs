using CCAT.Mvp1.Api.DTOs.VehiculosNuevos;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IVehiculoNuevoRepository
{
    Task<List<VehiculoNuevoResponse>> ListarAsync(string? q, bool? activo);
    Task<VehiculoNuevoResponse?> ObtenerPorIdAsync(int idVehiculo);

    Task<int> CrearAsync(VehiculoNuevoCrearRequest req);
    Task<int> ActualizarAsync(int idVehiculo, VehiculoNuevoActualizarRequest req);
    Task<int> CambiarEstadoAsync(int idVehiculo, bool activo, string usuario);

    Task AplicarMovimientoStockAsync(VehiculoNuevoStockMovimientoRequest req);
}
