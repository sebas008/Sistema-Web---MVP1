using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _service;
    public ProductosController(IProductoService service) => _service = service;

    [HttpPost]
    public Task<ProductoResponse> Crear([FromBody] ProductoCrearRequest req)
        => _service.CrearAsync(req);

    [HttpGet]
    public Task<List<ProductoResponse>> Listar([FromQuery] string? q, [FromQuery] bool? activo)
        => _service.ListarAsync(q, activo);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var p = await _service.ObtenerPorIdAsync(id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPut("{id:int}")]
    public Task<ProductoResponse> Actualizar(int id, [FromBody] ProductoActualizarRequest req)
        => _service.ActualizarAsync(id, req);

    [HttpPatch("{id:int}/estado")]
    public Task<ProductoResponse> CambiarEstado(int id, [FromQuery] bool activo)
        => _service.CambiarEstadoAsync(id, activo);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _service.EliminarAsync(id);
        return Ok(new { mensaje = "Producto eliminado correctamente." });
    }
}
