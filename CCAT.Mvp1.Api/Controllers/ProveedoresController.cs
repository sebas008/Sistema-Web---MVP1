using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Proveedores;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/contabilidad/proveedores")]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _service;
    public ProveedoresController(IProveedorService service) => _service = service;

    [HttpGet]
    public Task<List<ProveedorResponse>> Listar([FromQuery] string? q, [FromQuery] bool? activo)
        => _service.ListarAsync(q, activo);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var p = await _service.ObtenerAsync(id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public Task<ProveedorResponse> Crear([FromBody] ProveedorUpsertRequest req)
    {
        req.IdProveedor = null;
        return _service.UpsertAsync(req);
    }

    [HttpPut("{id:int}")]
    public Task<ProveedorResponse> Actualizar(int id, [FromBody] ProveedorUpsertRequest req)
    {
        req.IdProveedor = id;
        return _service.UpsertAsync(req);
    }
}
