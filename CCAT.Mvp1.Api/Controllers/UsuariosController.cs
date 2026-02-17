using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Usuarios;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;
    public UsuariosController(IUsuarioService service) => _service = service;

    [HttpPost]
    public Task<UsuarioResponse> Crear([FromBody] UsuarioCrearRequest req)
        => _service.CrearAsync(req);

    [HttpGet]
    public Task<List<UsuarioResponse>> Listar([FromQuery] bool? activo, [FromQuery] string? q)
        => _service.ListarAsync(activo, q);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var u = await _service.ObtenerPorIdAsync(id);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpGet("by-username/{username}")]
    public async Task<IActionResult> ObtenerPorUsername(string username)
    {
        var u = await _service.ObtenerPorUsernamePublicAsync(username);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPut("{id:int}")]
    public Task<UsuarioResponse> Actualizar(int id, [FromBody] UsuarioActualizarRequest req)
        => _service.ActualizarAsync(id, req);

    [HttpPatch("{id:int}/estado")]
    public Task<UsuarioResponse> CambiarEstado(int id, [FromBody] UsuarioCambiarEstadoRequest req)
        => _service.CambiarEstadoAsync(id, req);

    [HttpPatch("{id:int}/password")]
    public async Task<IActionResult> CambiarPassword(int id, [FromBody] UsuarioCambiarPasswordRequest req)
    {
        await _service.CambiarPasswordAsync(id, req);
        return Ok(new { ok = true });
    }

    [HttpPatch("{id:int}/rol")]
    public async Task<IActionResult> AsignarRol(int id, [FromBody] UsuarioAsignarRolRequest req)
    {
        await _service.AsignarRolAsync(id, req);
        return Ok(new { ok = true });
    }
}
