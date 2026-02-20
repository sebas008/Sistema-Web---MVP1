using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/ordenes-servicio")]
public class OrdenesServicioController : ControllerBase
{
    private readonly IOrdenServicioService _svc;
    public OrdenesServicioController(IOrdenServicioService svc) => _svc = svc;

    [HttpGet]
    public Task<List<OrdenServicioResponse>> Listar([FromQuery] string? q, [FromQuery] string? estado)
        => _svc.ListarAsync(q, estado);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var os = await _svc.ObtenerAsync(id);
        return os is null ? NotFound() : Ok(os);
    }

    [HttpPost]
    public Task<OrdenServicioResponse> Crear([FromBody] OrdenServicioCrearRequest req)
        => _svc.CrearAsync(req);

    [HttpPatch("{id:int}/estado")]
    public Task<OrdenServicioResponse> CambiarEstado(int id, [FromBody] OrdenServicioCambiarEstadoRequest req)
        => _svc.CambiarEstadoAsync(id, req.Estado);

    [HttpPost("{id:int}/detalle")]
    public Task<OrdenServicioResponse> AgregarDetalle(int id, [FromBody] OrdenServicioDetalleAddRequest req)
        => _svc.AgregarDetalleAsync(id, req);

    [HttpDelete("detalle/{idDetalle:int}")]
    public async Task<IActionResult> RemoverDetalle(int idDetalle, [FromQuery] string usuario = "admin")
    {
        await _svc.RemoverDetalleAsync(idDetalle, usuario);
        return Ok(new { ok = true });
    }
}