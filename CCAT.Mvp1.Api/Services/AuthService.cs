using CCAT.Mvp1.Api.Models;
using CCAT.Mvp1.Api.Repositories;

namespace CCAT.Mvp1.Api.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;

    public AuthService(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public LoginResponse Login(LoginRequest request)
    {
        // Aquí va la lógica de negocio (validaciones, normalización, etc.)
        // Por ahora MVP: delega al repo (que valida credenciales)
        return _authRepository.Login(request);
    }
}