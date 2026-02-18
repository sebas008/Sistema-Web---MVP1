using CCAT.Mvp1.Api.DTOs.VehiculosNuevos;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IVehiculoNuevoService
{
    Task<List<VehiculoNuevoResponse>> ListarAsync(string? q, bool? activo);
    Task<VehiculoNuevoResponse?> ObtenerPorIdAsync(int idVehiculo);

    Task<VehiculoNuevoResponse> CrearAsync(VehiculoNuevoCrearRequest req);
    Task<VehiculoNuevoResponse> ActualizarAsync(int idVehiculo, VehiculoNuevoActualizarRequest req);
    Task<VehiculoNuevoResponse> CambiarEstadoAsync(int idVehiculo, VehiculoNuevoCambiarEstadoRequest req);

    Task AplicarMovimientoStockAsync(VehiculoNuevoStockMovimientoRequest req);
}
