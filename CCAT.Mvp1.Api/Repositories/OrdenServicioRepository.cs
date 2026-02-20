using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class OrdenServicioRepository : IOrdenServicioRepository
{
    private readonly IDbConnectionFactory _factory;
    public OrdenServicioRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<List<OrdenServicioResponse>> ListarAsync(string? q, string? estado)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("servicios.usp_OrdenServicio_List", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@Q", (object?)q ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Estado", (object?)estado ?? DBNull.Value);

        var list = new List<OrdenServicioResponse>();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(MapCabecera(rd));
        }
        return list;
    }

    public async Task<OrdenServicioResponse?> ObtenerAsync(int idOrdenServicio)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("servicios.usp_OrdenServicio_Get", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdOrdenServicio", idOrdenServicio);

        using var rd = await cmd.ExecuteReaderAsync();
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
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("servicios.usp_OrdenServicio_Crear", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdCliente", (object?)req.IdCliente ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Placa", (object?)req.Placa ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Marca", (object?)req.Marca ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Modelo", (object?)req.Modelo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Kilometraje", (object?)req.Kilometraje ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Observacion", (object?)req.Observacion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Usuario", req.Usuario);

        using var rd = await cmd.ExecuteReaderAsync();
        await rd.ReadAsync();

        // SP retorna: IdOrdenServicio, NumeroOS
        var id = rd.GetInt32(rd.GetOrdinal("IdOrdenServicio"));
        var numero = rd.IsDBNull(rd.GetOrdinal("NumeroOS")) ? null : rd.GetString(rd.GetOrdinal("NumeroOS"));

        return (id, numero);
    }

    public async Task CambiarEstadoAsync(int idOrdenServicio, string estado)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("servicios.usp_OrdenServicio_CambiarEstado", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdOrdenServicio", idOrdenServicio);
        cmd.Parameters.AddWithValue("@Estado", estado);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(int idDetalle, int item)> AgregarDetalleAsync(int idOrdenServicio, OrdenServicioDetalleAddRequest req)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("servicios.usp_OrdenServicioDetalle_Add", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdOrdenServicio", idOrdenServicio);
        cmd.Parameters.AddWithValue("@Tipo", req.Tipo);
        cmd.Parameters.AddWithValue("@IdProducto", (object?)req.IdProducto ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Descripcion", req.Descripcion);
        cmd.Parameters.AddWithValue("@Cantidad", req.Cantidad);
        cmd.Parameters.AddWithValue("@PrecioUnitario", req.PrecioUnitario);
        cmd.Parameters.AddWithValue("@Usuario", req.Usuario);

        using var rd = await cmd.ExecuteReaderAsync();
        await rd.ReadAsync();

        // SP retorna: IdOrdenServicioDetalle, Item
        var idDet = rd.GetInt32(rd.GetOrdinal("IdOrdenServicioDetalle"));
        var item = rd.GetInt32(rd.GetOrdinal("Item"));

        return (idDet, item);
    }

    public async Task RemoverDetalleAsync(int idOrdenServicioDetalle, string usuario)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("servicios.usp_OrdenServicioDetalle_Remove", (SqlConnection)cn)
        { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdOrdenServicioDetalle", idOrdenServicioDetalle);
        cmd.Parameters.AddWithValue("@Usuario", usuario);

        await cmd.ExecuteNonQueryAsync();
    }

    private static OrdenServicioResponse MapCabecera(SqlDataReader rd)
    {
        return new OrdenServicioResponse
        {
            IdOrdenServicio = rd.GetInt32(rd.GetOrdinal("IdOrdenServicio")),
            NumeroOS = rd.IsDBNull(rd.GetOrdinal("NumeroOS")) ? null : rd.GetString(rd.GetOrdinal("NumeroOS")),
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