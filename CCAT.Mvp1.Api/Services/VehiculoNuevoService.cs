using CCAT.Mvp1.Api.DTOs.VehiculosNuevos;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class VehiculoNuevoService : IVehiculoNuevoService
{
    private readonly IVehiculoNuevoRepository _repo;

    public VehiculoNuevoService(IVehiculoNuevoRepository repo)
    {
        _repo = repo;
    }

    public Task<List<VehiculoNuevoResponse>> ListarAsync(string? q, bool? activo)
        => _repo.ListarAsync(q, activo);

    public Task<VehiculoNuevoResponse?> ObtenerPorIdAsync(int idVehiculo)
    {
        if (idVehiculo <= 0) throw new ArgumentException("IdVehiculo inválido.");
        return _repo.ObtenerPorIdAsync(idVehiculo);
    }

    public async Task<VehiculoNuevoResponse> CrearAsync(VehiculoNuevoCrearRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Marca)) throw new ArgumentException("Marca obligatoria.");
        if (string.IsNullOrWhiteSpace(req.Modelo)) throw new ArgumentException("Modelo obligatorio.");
        if (req.Anio <= 1900) throw new ArgumentException("Año inválido.");
        if (req.PrecioLista < 0) throw new ArgumentException("Precio inválido.");

        var id = await _repo.CrearAsync(req);
        return (await _repo.ObtenerPorIdAsync(id)) ?? throw new Exception("No se pudo leer el vehículo creado.");
    }

    public async Task<VehiculoNuevoResponse> ActualizarAsync(int idVehiculo, VehiculoNuevoActualizarRequest req)
    {
        if (idVehiculo <= 0) throw new ArgumentException("IdVehiculo inválido.");
        var id = await _repo.ActualizarAsync(idVehiculo, req);
        return (await _repo.ObtenerPorIdAsync(id)) ?? throw new Exception("No se pudo leer el vehículo actualizado.");
    }

    public async Task<VehiculoNuevoResponse> CambiarEstadoAsync(int idVehiculo, VehiculoNuevoCambiarEstadoRequest req)
    {
        if (idVehiculo <= 0) throw new ArgumentException("IdVehiculo inválido.");
        var id = await _repo.CambiarEstadoAsync(idVehiculo, req.Activo, req.Usuario);
        return (await _repo.ObtenerPorIdAsync(id)) ?? throw new Exception("No se pudo leer el vehículo.");
    }

    public Task AplicarMovimientoStockAsync(VehiculoNuevoStockMovimientoRequest req)
    {
        if (req.IdVehiculo <= 0) throw new ArgumentException("IdVehiculo inválido.");
        if (req.Cantidad <= 0) throw new ArgumentException("Cantidad debe ser > 0.");
        if (string.IsNullOrWhiteSpace(req.TipoMovimiento)) throw new ArgumentException("TipoMovimiento obligatorio.");

        req.TipoMovimiento = req.TipoMovimiento.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(req.Usuario)) req.Usuario = "admin";
        if (string.IsNullOrWhiteSpace(req.Referencia)) req.Referencia = "API";

        return _repo.AplicarMovimientoStockAsync(req);
    }
}
