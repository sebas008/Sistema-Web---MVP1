using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/contabilidad/guias")]
public class GuiasController : ControllerBase
{
    private readonly IGuiasService _service;
    public GuiasController(IGuiasService service) => _service = service;

    [HttpGet]
    public Task<List<GuiaResponse>> Listar([FromQuery] string? q)
        => _service.ListarAsync(q);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var g = await _service.ObtenerPorIdAsync(id);
        return g is null ? NotFound() : Ok(g);
    }

    [HttpPost("emitir")]
    public Task<GuiaResponse> Emitir([FromBody] GuiaEmitirRequest req)
        => _service.EmitirAsync(req);
}