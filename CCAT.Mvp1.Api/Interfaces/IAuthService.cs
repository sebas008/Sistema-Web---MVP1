using CCAT.Mvp1.Api.DTOs.Auth;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginDto dto);
}
