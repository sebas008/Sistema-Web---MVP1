using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class InventarioRepuestoRepository : IInventarioRepuestoRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    // Stores (si tus nombres difieren, cambia solo aquí)
    private const string SP_LIST = "inventario.usp_StockProducto_List";
    private const string SP_GET = "inventario.usp_StockProducto_Get";
    private const string SP_MOV = "inventario.usp_Stock_AplicarMovimiento";

    public InventarioRepuestoRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    private static object DbOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    public async Task<List<StockProductoResponse>> ListarStockAsync(string? q)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_LIST, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@Q", DbOrNull(q));

        var list = new List<StockProductoResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new StockProductoResponse
            {
                IdProducto = rd.GetInt32(rd.GetOrdinal("IdProducto")),
                Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
                Codigo = rd.IsDBNull(rd.GetOrdinal("Codigo")) ? "" : rd.GetString(rd.GetOrdinal("Codigo")),
                CantidadActual = rd.GetDecimal(rd.GetOrdinal("CantidadActual")),
                Referencia = rd.IsDBNull(rd.GetOrdinal("Referencia")) ? null : rd.GetString(rd.GetOrdinal("Referencia")),
                FechaActualizacion = rd.IsDBNull(rd.GetOrdinal("FechaActualizacion")) ? null : rd.GetDateTime(rd.GetOrdinal("FechaActualizacion"))
            });
        }

        return list;
    }

    public async Task<StockProductoResponse?> ObtenerStockAsync(int idProducto)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_GET, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@IdProducto", idProducto);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new StockProductoResponse
        {
            IdProducto = rd.GetInt32(rd.GetOrdinal("IdProducto")),
            Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
            Codigo = rd.IsDBNull(rd.GetOrdinal("Codigo")) ? "" : rd.GetString(rd.GetOrdinal("Codigo")),
            CantidadActual = rd.GetDecimal(rd.GetOrdinal("CantidadActual")),
            Referencia = rd.IsDBNull(rd.GetOrdinal("Referencia")) ? null : rd.GetString(rd.GetOrdinal("Referencia")),
            FechaActualizacion = rd.IsDBNull(rd.GetOrdinal("FechaActualizacion")) ? null : rd.GetDateTime(rd.GetOrdinal("FechaActualizacion"))
        };
    }

    public async Task AplicarMovimientoAsync(StockProductoMovimientoRequest req)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_MOV, cn) { CommandType = CommandType.StoredProcedure };

        // ⚠️ Ajusta estos nombres si tu SP usa otros:
        cmd.Parameters.AddWithValue("@IdProducto", req.IdProducto);
        cmd.Parameters.AddWithValue("@Cantidad", req.Cantidad);
        cmd.Parameters.AddWithValue("@TipoMovimiento", DbOrNull(req.TipoMovimiento));
        cmd.Parameters.AddWithValue("@Referencia", DbOrNull(req.Referencia));
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        await cmd.ExecuteNonQueryAsync();
    }
}
