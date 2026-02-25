using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Roles;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

// Implementación alineada al script (tabla seguridad.Rol)
public class RolRepository : IRolRepository
{
    private readonly IDbConnectionFactory _factory;
    public RolRepository(IDbConnectionFactory factory) => _factory = factory;

    private static object DbOrNull(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : v;

    public async Task<List<RolResponse>> ListarAsync(bool? soloActivos)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT TOP (200) IdRol, Nombre, Descripcion, Activo
FROM seguridad.Rol
WHERE (@solo IS NULL OR Activo = @solo)
ORDER BY Nombre;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@solo", (object?)soloActivos ?? DBNull.Value);

        var list = new List<RolResponse>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new RolResponse
            {
                IdRol = rd.GetInt32(rd.GetOrdinal("IdRol")),
                Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
                Descripcion = rd.IsDBNull(rd.GetOrdinal("Descripcion")) ? null : rd.GetString(rd.GetOrdinal("Descripcion")),
                Activo = rd.GetBoolean(rd.GetOrdinal("Activo"))
            });
        }
        return list;
    }

    public async Task<RolResponse> UpsertAsync(RolUpsertRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        if (req.IdRol is null)
        {
            var ins = @"
INSERT INTO seguridad.Rol (Nombre, Descripcion, Activo, FechaCreacion, UsuarioCreacion)
VALUES (@nombre, @desc, @activo, SYSDATETIME(), @usuario);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            await using var cmd = new SqlCommand(ins, (SqlConnection)cn);
            cmd.Parameters.AddWithValue("@nombre", req.Nombre);
            cmd.Parameters.AddWithValue("@desc", DbOrNull(req.Descripcion));
            cmd.Parameters.AddWithValue("@activo", req.Activo);
            cmd.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");
            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            return (await ObtenerAsync(newId))!;
        }
        else
        {
            var upd = @"
UPDATE seguridad.Rol
SET Nombre=@nombre,
    Descripcion=@desc,
    Activo=@activo,
    FechaActualizacion=SYSDATETIME(),
    UsuarioActualizacion=@usuario
WHERE IdRol=@id;";

            await using var cmd = new SqlCommand(upd, (SqlConnection)cn);
            cmd.Parameters.AddWithValue("@id", req.IdRol.Value);
            cmd.Parameters.AddWithValue("@nombre", req.Nombre);
            cmd.Parameters.AddWithValue("@desc", DbOrNull(req.Descripcion));
            cmd.Parameters.AddWithValue("@activo", req.Activo);
            cmd.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");
            await cmd.ExecuteNonQueryAsync();
            return (await ObtenerAsync(req.IdRol.Value))!;
        }
    }

    private async Task<RolResponse?> ObtenerAsync(int id)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();
        var sql = "SELECT IdRol, Nombre, Descripcion, Activo FROM seguridad.Rol WHERE IdRol=@id;";
        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;
        return new RolResponse
        {
            IdRol = rd.GetInt32(rd.GetOrdinal("IdRol")),
            Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
            Descripcion = rd.IsDBNull(rd.GetOrdinal("Descripcion")) ? null : rd.GetString(rd.GetOrdinal("Descripcion")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo"))
        };
    }
}
