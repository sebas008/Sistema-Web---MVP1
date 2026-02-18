using CCAT.Mvp1.Api.DTOs.Auth;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IAuthRepository
{
    Task<LoginResponse> LoginAsync(LoginDto dto);
}
