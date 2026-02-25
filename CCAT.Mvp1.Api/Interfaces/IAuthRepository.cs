using CCAT.Mvp1.Api.Models;

namespace CCAT.Mvp1.Api.Repositories;

public interface IAuthRepository
{
    LoginResponse Login(LoginRequest request);
}