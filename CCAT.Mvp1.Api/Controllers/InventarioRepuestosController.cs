using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/inventario/repuestos")]
public class InventarioRepuestosController : ControllerBase
{
    private readonly IInventarioRepuestoService _service;

    public InventarioRepuestosController(IInventarioRepuestoService service)
    {
        _service = service;
    }

    // GET /api/inventario/repuestos/stock?q=REP-01
    [HttpGet("stock")]
    public Task<List<StockProductoResponse>> Listar([FromQuery] string? q)
        => _service.ListarStockAsync(q);

    // GET /api/inventario/repuestos/stock/1
    [HttpGet("stock/{idProducto:int}")]
    public async Task<IActionResult> Get(int idProducto)
    {
        var item = await _service.ObtenerStockAsync(idProducto);
        return item is null ? NotFound() : Ok(item);
    }

    // POST /api/inventario/repuestos/movimiento
    [HttpPost("movimiento")]
    public async Task<IActionResult> Movimiento([FromBody] StockProductoMovimientoRequest req)
    {
        await _service.AplicarMovimientoAsync(req);
        return Ok(new { ok = true });
    }
}
