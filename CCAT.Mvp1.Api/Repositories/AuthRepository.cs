using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Auth;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    public AuthRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    public async Task<LoginResponse> LoginAsync(LoginDto dto)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand("seguridad.usp_Usuario_Login", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Username", dto.Username);
        cmd.Parameters.AddWithValue("@PasswordPlain", dto.Password);

        using var rd = await cmd.ExecuteReaderAsync();

        // SP puede lanzar error "Credenciales inválidas."
        // Si no leyera nada, igual controlamos.
        if (!await rd.ReadAsync())
            throw new Exception("Credenciales inválidas.");

        var resp = new LoginResponse
        {
            UsuarioId = rd.GetInt32(rd.GetOrdinal("IdUsuario")),
            Username = rd.GetString(rd.GetOrdinal("Username")),
            Nombres = rd.IsDBNull(rd.GetOrdinal("Nombres")) ? "" : rd.GetString(rd.GetOrdinal("Nombres")),
            Apellidos = rd.IsDBNull(rd.GetOrdinal("Apellidos")) ? "" : rd.GetString(rd.GetOrdinal("Apellidos")),
            Roles = new List<string>()
        };

        // 2do resultset: roles (columna "Nombre")
        if (await rd.NextResultAsync())
        {
            while (await rd.ReadAsync())
            {
                resp.Roles.Add(rd.GetString(rd.GetOrdinal("Nombre")));
            }
        }

        return resp;
    }
}
