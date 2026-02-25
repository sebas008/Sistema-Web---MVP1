using CCAT.Mvp1.Api.DTOs.Contabilidad.Proveedores;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class ProveedorService : IProveedorService
{
    private readonly IProveedorRepository _repo;
    public ProveedorService(IProveedorRepository repo) => _repo = repo;

    public Task<List<ProveedorResponse>> ListarAsync(string? q, bool? activo) => _repo.ListarAsync(q, activo);
    public Task<ProveedorResponse?> ObtenerAsync(int idProveedor) => _repo.ObtenerAsync(idProveedor);
    public Task<ProveedorResponse> UpsertAsync(ProveedorUpsertRequest req) => _repo.UpsertAsync(req);
}
