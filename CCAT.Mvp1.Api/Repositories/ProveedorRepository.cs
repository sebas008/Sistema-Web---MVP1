using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Proveedores;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly IDbConnectionFactory _factory;
    public ProveedorRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<List<ProveedorResponse>> ListarAsync(string? q, bool? activo)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT TOP (200)
    p.IdProveedor,
    p.RUC,
    p.RazonSocial,
    p.Direccion,
    p.Telefono,
    p.Email,
    p.Activo
FROM contabilidad.Proveedor p
WHERE (@activo IS NULL OR p.Activo = @activo)
  AND (
        @q IS NULL
        OR p.RazonSocial LIKE '%' + @q + '%'
        OR p.RUC LIKE '%' + @q + '%'
      )
ORDER BY p.RazonSocial;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@q", (object?)q ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@activo", (object?)activo ?? DBNull.Value);

        var list = new List<ProveedorResponse>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new ProveedorResponse
            {
                IdProveedor = rd.GetInt32(0),
                Ruc = rd.IsDBNull(1) ? null : rd.GetString(1),
                RazonSocial = rd.GetString(2),
                Direccion = rd.IsDBNull(3) ? null : rd.GetString(3),
                Telefono = rd.IsDBNull(4) ? null : rd.GetString(4),
                Email = rd.IsDBNull(5) ? null : rd.GetString(5),
                Activo = rd.GetBoolean(6)
            });
        }
        return list;
    }

    public async Task<ProveedorResponse?> ObtenerAsync(int idProveedor)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT
    p.IdProveedor,
    p.RUC,
    p.RazonSocial,
    p.Direccion,
    p.Telefono,
    p.Email,
    p.Activo
FROM contabilidad.Proveedor p
WHERE p.IdProveedor = @id;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", idProveedor);

        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new ProveedorResponse
        {
            IdProveedor = rd.GetInt32(0),
            Ruc = rd.IsDBNull(1) ? null : rd.GetString(1),
            RazonSocial = rd.GetString(2),
            Direccion = rd.IsDBNull(3) ? null : rd.GetString(3),
            Telefono = rd.IsDBNull(4) ? null : rd.GetString(4),
            Email = rd.IsDBNull(5) ? null : rd.GetString(5),
            Activo = rd.GetBoolean(6)
        };
    }

    public async Task<ProveedorResponse> UpsertAsync(ProveedorUpsertRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        // Sin SP en el script. Hacemos upsert simple.
        if (req.IdProveedor is null)
        {
            var ins = @"
INSERT INTO contabilidad.Proveedor (RUC, RazonSocial, Direccion, Telefono, Email, Activo, FechaCreacion, UsuarioCreacion)
VALUES (@ruc, @razon, @dir, @tel, @email, @activo, SYSDATETIME(), @usuario);
SELECT SCOPE_IDENTITY();";

            await using var cmd = new SqlCommand(ins, (SqlConnection)cn);
            cmd.Parameters.AddWithValue("@ruc", (object?)req.Ruc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@razon", req.RazonSocial);
            cmd.Parameters.AddWithValue("@dir", (object?)req.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tel", (object?)req.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)req.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@activo", req.Activo);
            cmd.Parameters.AddWithValue("@usuario", req.Usuario);
            var newIdObj = await cmd.ExecuteScalarAsync();
            var newId = Convert.ToInt32(newIdObj);
            var created = await ObtenerAsync(newId);
            return created!;
        }
        else
        {
            var upd = @"
UPDATE contabilidad.Proveedor
SET RUC=@ruc, RazonSocial=@razon, Direccion=@dir, Telefono=@tel, Email=@email, Activo=@activo
WHERE IdProveedor=@id;";

            await using var cmd = new SqlCommand(upd, (SqlConnection)cn);
            cmd.Parameters.AddWithValue("@id", req.IdProveedor.Value);
            cmd.Parameters.AddWithValue("@ruc", (object?)req.Ruc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@razon", req.RazonSocial);
            cmd.Parameters.AddWithValue("@dir", (object?)req.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tel", (object?)req.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)req.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@activo", req.Activo);
            await cmd.ExecuteNonQueryAsync();
            var updated = await ObtenerAsync(req.IdProveedor.Value);
            return updated!;
        }
    }
}
