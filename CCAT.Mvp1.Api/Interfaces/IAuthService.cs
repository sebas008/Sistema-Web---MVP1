using CCAT.Mvp1.Api.Models;

namespace CCAT.Mvp1.Api.Services;

public interface IAuthService
{
    LoginResponse Login(LoginRequest request);
}