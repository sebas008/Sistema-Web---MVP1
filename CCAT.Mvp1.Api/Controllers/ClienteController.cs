using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Clientes;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _svc;
    public ClientesController(IClienteService svc) => _svc = svc;

    [HttpGet]
    public Task<List<ClienteResponse>> Listar([FromQuery] string? q, [FromQuery] bool? soloActivos)
        => _svc.ListarAsync(q, soloActivos);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var item = await _svc.ObtenerAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public Task<ClienteResponse> Crear([FromBody] ClienteCrearRequest req)
        => _svc.CrearAsync(req);

    [HttpPut("{id:int}")]
    public Task<ClienteResponse> Actualizar(int id, [FromBody] ClienteActualizarRequest req)
        => _svc.ActualizarAsync(id, req);

    [HttpPatch("{id:int}/estado")]
    public Task<ClienteResponse> CambiarEstado(int id, [FromBody] ClienteCambiarEstadoRequest req)
        => _svc.CambiarEstadoAsync(id, req.Activo);
}