using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Roles;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly IRolService _svc;
    public RolesController(IRolService svc) => _svc = svc;

    [HttpGet]
    public Task<List<RolResponse>> Listar([FromQuery] bool? soloActivos)
        => _svc.ListarAsync(soloActivos);

    [HttpPost]
    public Task<RolResponse> Crear([FromBody] RolUpsertRequest req)
    {
        req.IdRol = null;
        return _svc.UpsertAsync(req);
    }

    [HttpPut("{id:int}")]
    public Task<RolResponse> Actualizar(int id, [FromBody] RolUpsertRequest req)
    {
        req.IdRol = id;
        return _svc.UpsertAsync(req);
    }
}
