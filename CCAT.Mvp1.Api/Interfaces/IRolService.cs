using CCAT.Mvp1.Api.DTOs.Roles;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IRolService
{
    Task<List<RolResponse>> ListarAsync(bool? soloActivos);
    Task<RolResponse> UpsertAsync(RolUpsertRequest req);
}
