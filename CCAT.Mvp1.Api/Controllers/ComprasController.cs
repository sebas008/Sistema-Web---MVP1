using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/contabilidad/compras")]
public class ComprasController : ControllerBase
{
    private readonly IComprasService _service;
    public ComprasController(IComprasService service) => _service = service;

    [HttpGet]
    public Task<List<CompraResponse>> Listar([FromQuery] string? q)
        => _service.ListarAsync(q);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var c = await _service.ObtenerPorIdAsync(id);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost("registrar")]
    public Task<CompraResponse> Registrar([FromBody] CompraRegistrarRequest req)
        => _service.RegistrarAsync(req);
}