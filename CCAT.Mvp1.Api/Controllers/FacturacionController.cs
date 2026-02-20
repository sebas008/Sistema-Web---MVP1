using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/contabilidad/facturas")]
public class FacturacionController : ControllerBase
{
    private readonly IFacturacionService _service;
    public FacturacionController(IFacturacionService service) => _service = service;

    [HttpGet]
    public Task<List<FacturaResponse>> Listar([FromQuery] string? q)
        => _service.ListarAsync(q);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var f = await _service.ObtenerPorIdAsync(id);
        return f is null ? NotFound() : Ok(f);
    }

    [HttpPost("emitir")]
    public Task<FacturaResponse> Emitir([FromBody] FacturaEmitirRequest req)
        => _service.EmitirAsync(req);
}