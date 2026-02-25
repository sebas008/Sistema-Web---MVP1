using CCAT.Mvp1.Api.DTOs.Contabilidad.Proveedores;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IProveedorService
{
    Task<List<ProveedorResponse>> ListarAsync(string? q, bool? activo);
    Task<ProveedorResponse?> ObtenerAsync(int idProveedor);
    Task<ProveedorResponse> UpsertAsync(ProveedorUpsertRequest req);
}
