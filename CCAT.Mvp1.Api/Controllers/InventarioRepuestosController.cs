using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/inventario/repuestos")]
public class InventarioRepuestosController : ControllerBase
{
    private readonly IInventarioRepuestoService _service;
    private readonly IProductoService _productoService;

    public InventarioRepuestosController(IInventarioRepuestoService service, IProductoService productoService)
    {
        _service = service;
        _productoService = productoService;
    }

    [HttpGet("stock")]
    public Task<List<StockProductoResponse>> Listar([FromQuery] string? q)
        => _service.ListarStockAsync(q);

    [HttpGet("stock/{idProducto:int}")]
    public async Task<IActionResult> Get(int idProducto)
    {
        var item = await _service.ObtenerStockAsync(idProducto);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("movimiento")]
    public async Task<IActionResult> Movimiento([FromBody] StockProductoMovimientoRequest req)
    {
        await _service.AplicarMovimientoAsync(req);
        return Ok(new { ok = true });
    }

    [HttpDelete("{idProducto:int}")]
    [HttpDelete("stock/{idProducto:int}")]
    public async Task<IActionResult> Eliminar(int idProducto)
    {
        await _productoService.EliminarAsync(idProducto);
        return Ok(new { mensaje = "Producto eliminado correctamente." });
    }
}
