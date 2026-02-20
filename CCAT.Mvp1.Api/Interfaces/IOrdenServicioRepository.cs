using CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IOrdenServicioRepository
{
    Task<List<OrdenServicioResponse>> ListarAsync(string? q, string? estado);
    Task<OrdenServicioResponse?> ObtenerAsync(int idOrdenServicio);

    Task<(int idOrdenServicio, string? numeroOS)> CrearAsync(OrdenServicioCrearRequest req);
    Task CambiarEstadoAsync(int idOrdenServicio, string estado);

    Task<(int idDetalle, int item)> AgregarDetalleAsync(int idOrdenServicio, OrdenServicioDetalleAddRequest req);
    Task RemoverDetalleAsync(int idOrdenServicioDetalle, string usuario);
}