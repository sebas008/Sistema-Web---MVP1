using CCAT.Mvp1.Api.DTOs.Auth;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;

    public AuthService(IAuthRepository repo)
    {
        _repo = repo;
    }

    public Task<LoginResponse> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new ArgumentException("Username obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Password obligatorio.");

        return _repo.LoginAsync(dto);
    }
}
