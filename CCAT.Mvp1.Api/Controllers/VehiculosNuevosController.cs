using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.VehiculosNuevos;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/vehiculos-nuevos")]
public class VehiculosNuevosController : ControllerBase
{
    private readonly IVehiculoNuevoService _service;

    public VehiculosNuevosController(IVehiculoNuevoService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<List<VehiculoNuevoResponse>> Listar([FromQuery] string? q, [FromQuery] bool? activo)
        => _service.ListarAsync(q, activo);

    [HttpGet("{idVehiculo:int}")]
    public async Task<IActionResult> Get(int idVehiculo)
    {
        var v = await _service.ObtenerPorIdAsync(idVehiculo);
        return v is null ? NotFound() : Ok(v);
    }

    [HttpPost]
    public Task<VehiculoNuevoResponse> Crear([FromBody] VehiculoNuevoCrearRequest req)
        => _service.CrearAsync(req);

    [HttpPut("{idVehiculo:int}")]
    public Task<VehiculoNuevoResponse> Actualizar(int idVehiculo, [FromBody] VehiculoNuevoActualizarRequest req)
        => _service.ActualizarAsync(idVehiculo, req);

    [HttpPatch("{idVehiculo:int}/estado")]
    public Task<VehiculoNuevoResponse> CambiarEstado(int idVehiculo, [FromBody] VehiculoNuevoCambiarEstadoRequest req)
        => _service.CambiarEstadoAsync(idVehiculo, req);

    [HttpPost("{idVehiculo:int}/stock/movimiento")]
    public async Task<IActionResult> MovimientoStock(int idVehiculo, [FromBody] VehiculoNuevoStockMovimientoRequest req)
    {
        req.IdVehiculo = idVehiculo;
        await _service.AplicarMovimientoStockAsync(req);
        return Ok(new { ok = true });
    }
}
