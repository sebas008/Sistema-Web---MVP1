using CCAT.Mvp1.Api.Interfaces;
using CCAT.Mvp1.Api.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CCAT.Mvp1.Api.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _factory;

    public AuthRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public LoginResponse Login(LoginRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new UnauthorizedAccessException("Username obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedAccessException("Password obligatorio.");

        using var cn = _factory.CreateConnection();

        try
        {
            using var multi = cn.QueryMultiple(
                "seguridad.usp_Usuario_Login",
                new
                {
                    Username = request.Username.Trim(),
                    PasswordPlain = request.Password
                },
                commandType: CommandType.StoredProcedure
            );

            // 1er resultset: usuario
            var user = multi.ReadFirstOrDefault<UsuarioLoginRow>();
            if (user is null)
                throw new UnauthorizedAccessException("Credenciales inválidas.");

            // 2do resultset: roles
            var roles = multi.Read<RolRow>()
                .Select(r => r.Nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new LoginResponse
            {
                UsuarioId = user.IdUsuario,
                Username = user.Username,
                Nombres = user.Nombres ?? "",
                Apellidos = user.Apellidos ?? "",
                Roles = roles
            };
        }
        catch (SqlException ex)
        {
            // Tu SP THROW usa números 52301, 52302, 52303
            // 52301: credenciales inválidas
            // 52302: usuario inactivo
            // 52303: usuario bloqueado
            // En SqlException, el "Number" puede ser el que lanzas con THROW.
            if (ex.Number is 52301 or 52302 or 52303)
            {
                // devolvemos 401 desde el controller/middleware capturando UnauthorizedAccessException
                throw new UnauthorizedAccessException(ex.Message);
            }

            // Cualquier otro SQL error: 500 (no lo camuflamos como 401)
            throw;
        }
    }

    private sealed class UsuarioLoginRow
    {
        public int IdUsuario { get; set; }
        public string Username { get; set; } = "";
        public string? Email { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public bool Activo { get; set; }
    }

    private sealed class RolRow
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; } = "";
    }
}