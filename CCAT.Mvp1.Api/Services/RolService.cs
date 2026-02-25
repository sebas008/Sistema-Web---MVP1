using CCAT.Mvp1.Api.DTOs.Roles;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class RolService : IRolService
{
    private readonly IRolRepository _repo;
    public RolService(IRolRepository repo) => _repo = repo;

    public Task<List<RolResponse>> ListarAsync(bool? soloActivos) => _repo.ListarAsync(soloActivos);
    public Task<RolResponse> UpsertAsync(RolUpsertRequest req) => _repo.UpsertAsync(req);
}
