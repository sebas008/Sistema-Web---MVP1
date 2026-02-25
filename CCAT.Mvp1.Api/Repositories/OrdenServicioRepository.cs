using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

// Implementación alineada a SISTEMA_CCAT.sql (sin SPs de cabecera OS)
public class OrdenServicioRepository : IOrdenServicioRepository
{
    private readonly IDbConnectionFactory _factory;
    public OrdenServicioRepository(IDbConnectionFactory factory) => _factory = factory;

    private static object DbOrNull(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : v;

    public async Task<List<OrdenServicioResponse>> ListarAsync(string? q, string? estado)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT TOP (300)
    os.IdOrdenServicio,
    os.NumeroOS,
    os.IdCliente,
    os.Placa,
    os.Marca,
    os.Modelo,
    os.Kilometraje,
    os.FechaIngreso,
    os.FechaSalida,
    os.Estado,
    os.Observacion,
    os.Subtotal,
    os.IGV,
    os.Total,
    os.FechaCreacion,
    os.UsuarioCreacion
FROM servicios.OrdenServicio os
WHERE (@estado IS NULL OR os.Estado = @estado)
  AND (
        @q IS NULL
        OR os.NumeroOS LIKE '%' + @q + '%'
        OR os.Placa LIKE '%' + @q + '%'
        OR os.Marca LIKE '%' + @q + '%'
        OR os.Modelo LIKE '%' + @q + '%'
      )
ORDER BY os.IdOrdenServicio DESC;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@q", DbOrNull(q));
        cmd.Parameters.AddWithValue("@estado", DbOrNull(estado));

        var list = new List<OrdenServicioResponse>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(MapCabecera(rd));
        }
        return list;
    }

    public async Task<OrdenServicioResponse?> ObtenerAsync(int idOrdenServicio)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT
    os.IdOrdenServicio,
    os.NumeroOS,
    os.IdCliente,
    os.Placa,
    os.Marca,
    os.Modelo,
    os.Kilometraje,
    os.FechaIngreso,
    os.FechaSalida,
    os.Estado,
    os.Observacion,
    os.Subtotal,
    os.IGV,
    os.Total,
    os.FechaCreacion,
    os.UsuarioCreacion
FROM servicios.OrdenServicio os
WHERE os.IdOrdenServicio = @id;

SELECT
    d.IdOrdenServicioDetalle,
    d.IdOrdenServicio,
    d.Item,
    d.Tipo,
    d.IdProducto,
    d.Descripcion,
    d.Cantidad,
    d.PrecioUnitario,
    d.Importe
FROM servicios.OrdenServicioDetalle d
WHERE d.IdOrdenServicio = @id
ORDER BY d.Item;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", idOrdenServicio);

        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        var os = MapCabecera(rd);

        if (await rd.NextResultAsync())
        {
            while (await rd.ReadAsync())
            {
                os.Detalle.Add(MapDetalle(rd));
            }
        }

        return os;
    }

    public async Task<(int idOrdenServicio, string? numeroOS)> CrearAsync(OrdenServicioCrearRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        await using var tx = await ((SqlConnection)cn).BeginTransactionAsync();
        try
        {
            // NumeroOS: OS-YYYYMMDD-####
            var today = DateTime.Now;
            var prefix = $"OS-{today:yyyyMMdd}-";

            var seqSql = @"
SELECT ISNULL(MAX(CAST(RIGHT(NumeroOS, 4) AS INT)), 0) + 1
FROM servicios.OrdenServicio
WHERE NumeroOS LIKE @pref + '%';";

            int next;
            await using (var cmdSeq = new SqlCommand(seqSql, (SqlConnection)cn, (SqlTransaction)tx))
            {
                cmdSeq.Parameters.AddWithValue("@pref", prefix);
                next = Convert.ToInt32(await cmdSeq.ExecuteScalarAsync());
            }

            var numeroOS = prefix + next.ToString("0000");

            var insSql = @"
INSERT INTO servicios.OrdenServicio
(NumeroOS, IdCliente, Placa, Marca, Modelo, Kilometraje, FechaIngreso, Estado, Observacion, Subtotal, IGV, Total, FechaCreacion, UsuarioCreacion)
VALUES
(@numero, @idCliente, @placa, @marca, @modelo, @km, SYSDATETIME(), 'ABIERTA', @obs, 0, 0, 0, SYSDATETIME(), @usuario);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newId;
            await using (var cmd = new SqlCommand(insSql, (SqlConnection)cn, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@numero", numeroOS);
                cmd.Parameters.AddWithValue("@idCliente", (object?)req.IdCliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@placa", DbOrNull(req.Placa));
                cmd.Parameters.AddWithValue("@marca", DbOrNull(req.Marca));
                cmd.Parameters.AddWithValue("@modelo", DbOrNull(req.Modelo));
                cmd.Parameters.AddWithValue("@km", (object?)req.Kilometraje ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@obs", DbOrNull(req.Observacion));
                cmd.Parameters.AddWithValue("@usuario", DbOrNull(req.Usuario) ?? "admin");
                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            await tx.CommitAsync();
            return (newId, numeroOS);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task CambiarEstadoAsync(int idOrdenServicio, string estado)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = "UPDATE servicios.OrdenServicio SET Estado=@e WHERE IdOrdenServicio=@id;";
        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", idOrdenServicio);
        cmd.Parameters.AddWithValue("@e", estado);
        await cmd.ExecuteNonQueryAsync();
    }

    // Detalle usa SPs del script (existen)
    public async Task<(int idDetalle, int item)> AgregarDetalleAsync(int idOrdenServicio, OrdenServicioDetalleAddRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        await using var cmd = new SqlCommand("servicios.usp_OrdenServicioDetalle_Add", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdOrdenServicio", idOrdenServicio);
        cmd.Parameters.AddWithValue("@Tipo", req.Tipo);
        cmd.Parameters.AddWithValue("@IdProducto", (object?)req.IdProducto ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Descripcion", req.Descripcion);
        cmd.Parameters.AddWithValue("@Cantidad", req.Cantidad);
        cmd.Parameters.AddWithValue("@PrecioUnitario", req.PrecioUnitario);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario) ?? "admin");

        await using var rd = await cmd.ExecuteReaderAsync();
        await rd.ReadAsync();
        var idDet = rd.GetInt32(rd.GetOrdinal("IdOrdenServicioDetalle"));
        var item = rd.GetInt32(rd.GetOrdinal("Item"));
        return (idDet, item);
    }

    public async Task RemoverDetalleAsync(int idOrdenServicioDetalle, string usuario)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        await using var cmd = new SqlCommand("servicios.usp_OrdenServicioDetalle_Remove", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@IdOrdenServicioDetalle", idOrdenServicioDetalle);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(usuario) ?? "admin");
        await cmd.ExecuteNonQueryAsync();
    }

    private static OrdenServicioResponse MapCabecera(SqlDataReader rd)
    {
        return new OrdenServicioResponse
        {
            IdOrdenServicio = rd.GetInt32(rd.GetOrdinal("IdOrdenServicio")),
            NumeroOS = rd.GetString(rd.GetOrdinal("NumeroOS")),
            IdCliente = rd.IsDBNull(rd.GetOrdinal("IdCliente")) ? null : rd.GetInt32(rd.GetOrdinal("IdCliente")),
            Placa = rd.IsDBNull(rd.GetOrdinal("Placa")) ? null : rd.GetString(rd.GetOrdinal("Placa")),
            Marca = rd.IsDBNull(rd.GetOrdinal("Marca")) ? null : rd.GetString(rd.GetOrdinal("Marca")),
            Modelo = rd.IsDBNull(rd.GetOrdinal("Modelo")) ? null : rd.GetString(rd.GetOrdinal("Modelo")),
            Kilometraje = rd.IsDBNull(rd.GetOrdinal("Kilometraje")) ? null : rd.GetInt32(rd.GetOrdinal("Kilometraje")),
            FechaIngreso = rd.IsDBNull(rd.GetOrdinal("FechaIngreso")) ? null : rd.GetDateTime(rd.GetOrdinal("FechaIngreso")),
            FechaSalida = rd.IsDBNull(rd.GetOrdinal("FechaSalida")) ? null : rd.GetDateTime(rd.GetOrdinal("FechaSalida")),
            Estado = rd.GetString(rd.GetOrdinal("Estado")),
            Observacion = rd.IsDBNull(rd.GetOrdinal("Observacion")) ? null : rd.GetString(rd.GetOrdinal("Observacion")),
            Subtotal = rd.GetDecimal(rd.GetOrdinal("Subtotal")),
            IGV = rd.GetDecimal(rd.GetOrdinal("IGV")),
            Total = rd.GetDecimal(rd.GetOrdinal("Total")),
            FechaCreacion = rd.IsDBNull(rd.GetOrdinal("FechaCreacion")) ? null : rd.GetDateTime(rd.GetOrdinal("FechaCreacion")),
            UsuarioCreacion = rd.IsDBNull(rd.GetOrdinal("UsuarioCreacion")) ? null : rd.GetString(rd.GetOrdinal("UsuarioCreacion")),
        };
    }

    private static OrdenServicioDetalleResponse MapDetalle(SqlDataReader rd)
    {
        return new OrdenServicioDetalleResponse
        {
            IdOrdenServicioDetalle = rd.GetInt32(rd.GetOrdinal("IdOrdenServicioDetalle")),
            IdOrdenServicio = rd.GetInt32(rd.GetOrdinal("IdOrdenServicio")),
            Item = rd.GetInt32(rd.GetOrdinal("Item")),
            Tipo = rd.GetString(rd.GetOrdinal("Tipo")),
            IdProducto = rd.IsDBNull(rd.GetOrdinal("IdProducto")) ? null : rd.GetInt32(rd.GetOrdinal("IdProducto")),
            Descripcion = rd.GetString(rd.GetOrdinal("Descripcion")),
            Cantidad = rd.GetDecimal(rd.GetOrdinal("Cantidad")),
            PrecioUnitario = rd.GetDecimal(rd.GetOrdinal("PrecioUnitario")),
            Importe = rd.GetDecimal(rd.GetOrdinal("Importe")),
        };
    }
}
