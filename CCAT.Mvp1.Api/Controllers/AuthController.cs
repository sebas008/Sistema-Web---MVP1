using Microsoft.AspNetCore.Mvc;
using CCAT.Mvp1.Api.DTOs.Auth;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    [HttpPost("login")]
    public Task<LoginResponse> Login([FromBody] LoginDto dto)
        => _service.LoginAsync(dto);
}
