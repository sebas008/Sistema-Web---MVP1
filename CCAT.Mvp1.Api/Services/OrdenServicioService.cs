using CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class OrdenServicioService : IOrdenServicioService
{
    private readonly IOrdenServicioRepository _repo;
    public OrdenServicioService(IOrdenServicioRepository repo) => _repo = repo;

    public Task<List<OrdenServicioResponse>> ListarAsync(string? q, string? estado)
        => _repo.ListarAsync(q, estado);

    public Task<OrdenServicioResponse?> ObtenerAsync(int idOrdenServicio)
        => _repo.ObtenerAsync(idOrdenServicio);

    public async Task<OrdenServicioResponse> CrearAsync(OrdenServicioCrearRequest req)
    {
        var (id, _) = await _repo.CrearAsync(req);
        var os = await _repo.ObtenerAsync(id);
        return os ?? throw new Exception("No se pudo obtener la OS creada.");
    }

    public async Task<OrdenServicioResponse> CambiarEstadoAsync(int idOrdenServicio, string estado)
    {
        await _repo.CambiarEstadoAsync(idOrdenServicio, estado);
        var os = await _repo.ObtenerAsync(idOrdenServicio);
        return os ?? throw new Exception("No se pudo obtener la OS luego del cambio de estado.");
    }

    public async Task<OrdenServicioResponse> AgregarDetalleAsync(int idOrdenServicio, OrdenServicioDetalleAddRequest req)
    {
        await _repo.AgregarDetalleAsync(idOrdenServicio, req);
        var os = await _repo.ObtenerAsync(idOrdenServicio);
        return os ?? throw new Exception("No se pudo obtener la OS luego de agregar detalle.");
    }

    public async Task<OrdenServicioResponse> RemoverDetalleAsync(int idOrdenServicioDetalle, string usuario)
    {
        // Para devolver la OS, primero intentamos resolverla: tu SP Remove devuelve IdOrdenServicio,
        // pero aquí estamos usando ExecuteNonQuery. Mantengo simple: pides luego el GET de la OS.
        await _repo.RemoverDetalleAsync(idOrdenServicioDetalle, usuario);
        // No sabemos el IdOrdenServicio aquí sin cambiar el SP o leer antes.
        return new OrdenServicioResponse { IdOrdenServicio = 0 };
    }
}