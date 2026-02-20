using CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IOrdenServicioService
{
    Task<List<OrdenServicioResponse>> ListarAsync(string? q, string? estado);
    Task<OrdenServicioResponse?> ObtenerAsync(int idOrdenServicio);

    Task<OrdenServicioResponse> CrearAsync(OrdenServicioCrearRequest req);
    Task<OrdenServicioResponse> CambiarEstadoAsync(int idOrdenServicio, string estado);

    Task<OrdenServicioResponse> AgregarDetalleAsync(int idOrdenServicio, OrdenServicioDetalleAddRequest req);
    Task<OrdenServicioResponse> RemoverDetalleAsync(int idOrdenServicioDetalle, string usuario);
}