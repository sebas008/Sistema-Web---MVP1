using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class InventarioRepuestoRepository : IInventarioRepuestoRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    private const string SP_STOCK_MOV = "inventario.usp_Stock_AplicarMovimiento";

    public InventarioRepuestoRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    private static object DbOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    public async Task<List<StockProductoResponse>> ListarStockAsync(string? q)
    {
        await using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        var sql = @"
SELECT
    p.IdProducto,
    p.Nombre,
    p.TipoProducto,
    ISNULL(p.Codigo,'') AS Codigo,
    p.Precio,
    CAST(COALESCE(s.CantidadActual, sm.StockCalculado, 0) AS DECIMAL(18,2)) AS Stock,
    COALESCE(s.Referencia, sm.Referencia) AS Referencia,
    COALESCE(s.FechaActualizacion, sm.FechaMovimiento) AS FechaActualizacion
FROM inventario.Producto p
LEFT JOIN inventario.Stock s 
    ON s.IdProducto = p.IdProducto
OUTER APPLY (
    SELECT
        SUM(CASE WHEN sm.TipoMovimiento = 'IN' THEN sm.Cantidad ELSE -sm.Cantidad END) AS StockCalculado,
        MAX(sm.Referencia) AS Referencia,
        MAX(sm.FechaMovimiento) AS FechaMovimiento
    FROM inventario.StockMovimiento sm
    WHERE sm.IdProducto = p.IdProducto
) sm
WHERE p.Activo = 1
  AND (
        @q IS NULL
        OR p.Nombre LIKE '%' + @q + '%'
        OR ISNULL(p.Codigo,'') LIKE '%' + @q + '%'
        OR ISNULL(p.TipoProducto,'') LIKE '%' + @q + '%'
      )
ORDER BY p.IdProducto DESC;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@q", DbOrNull(q));

        var list = new List<StockProductoResponse>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new StockProductoResponse
            {
                IdProducto = rd.GetInt32(rd.GetOrdinal("IdProducto")),
                Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
                Categoria = rd.IsDBNull(rd.GetOrdinal("TipoProducto")) ? null : rd.GetString(rd.GetOrdinal("TipoProducto")),
                Codigo = rd.GetString(rd.GetOrdinal("Codigo")),
                Precio = rd.IsDBNull(rd.GetOrdinal("Precio")) ? null : rd.GetDecimal(rd.GetOrdinal("Precio")),
                Stock = rd.GetDecimal(rd.GetOrdinal("Stock")),
                Referencia = rd.IsDBNull(rd.GetOrdinal("Referencia")) ? null : rd.GetString(rd.GetOrdinal("Referencia")),
                FechaActualizacion = rd.IsDBNull(rd.GetOrdinal("FechaActualizacion")) ? null : rd.GetDateTime(rd.GetOrdinal("FechaActualizacion"))
            });
        }

        return list;
    }

    public async Task<StockProductoResponse?> ObtenerStockAsync(int idProducto)
    {
        await using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        var sql = @"
SELECT
    p.IdProducto,
    p.Nombre,
    p.TipoProducto,
    ISNULL(p.Codigo,'') AS Codigo,
    p.Precio,
    CAST(COALESCE(s.CantidadActual, sm.StockCalculado, 0) AS DECIMAL(18,2)) AS Stock,
    COALESCE(s.Referencia, sm.Referencia) AS Referencia,
    COALESCE(s.FechaActualizacion, sm.FechaMovimiento) AS FechaActualizacion
FROM inventario.Producto p
LEFT JOIN inventario.Stock s 
    ON s.IdProducto = p.IdProducto
OUTER APPLY (
    SELECT
        SUM(CASE WHEN sm.TipoMovimiento = 'IN' THEN sm.Cantidad ELSE -sm.Cantidad END) AS StockCalculado,
        MAX(sm.Referencia) AS Referencia,
        MAX(sm.FechaMovimiento) AS FechaMovimiento
    FROM inventario.StockMovimiento sm
    WHERE sm.IdProducto = p.IdProducto
) sm
WHERE p.IdProducto = @IdProducto;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@IdProducto", idProducto);

        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new StockProductoResponse
        {
            IdProducto = rd.GetInt32(rd.GetOrdinal("IdProducto")),
            Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
            Categoria = rd.IsDBNull(rd.GetOrdinal("TipoProducto")) ? null : rd.GetString(rd.GetOrdinal("TipoProducto")),
            Codigo = rd.GetString(rd.GetOrdinal("Codigo")),
            Precio = rd.IsDBNull(rd.GetOrdinal("Precio")) ? null : rd.GetDecimal(rd.GetOrdinal("Precio")),
            Stock = rd.GetDecimal(rd.GetOrdinal("Stock")),
            Referencia = rd.IsDBNull(rd.GetOrdinal("Referencia")) ? null : rd.GetString(rd.GetOrdinal("Referencia")),
            FechaActualizacion = rd.IsDBNull(rd.GetOrdinal("FechaActualizacion")) ? null : rd.GetDateTime(rd.GetOrdinal("FechaActualizacion"))
        };
    }

    public async Task AplicarMovimientoAsync(StockProductoMovimientoRequest req)
    {
        await using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        await using var cmd = new SqlCommand(SP_STOCK_MOV, (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdProducto", req.IdProducto);
        cmd.Parameters.AddWithValue("@TipoMovimiento", MapTipo(req.TipoMovimiento));
        cmd.Parameters.AddWithValue("@Cantidad", req.Cantidad);
        cmd.Parameters.AddWithValue("@Referencia", DbOrNull(req.Referencia));
        cmd.Parameters.AddWithValue("@Observacion",
            (req.TipoMovimiento ?? "").Trim().ToUpperInvariant() == "AJUSTE"
                ? "AJUSTE"
                : DBNull.Value);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        await cmd.ExecuteNonQueryAsync();
    }

    private static string MapTipo(string tipoMovimiento)
    {
        var t = (tipoMovimiento ?? "").Trim().ToUpperInvariant();
        return t switch
        {
            "IN" => "IN",
            "OUT" => "OUT",
            "ENTRADA" => "IN",
            "SALIDA" => "OUT",
            "AJUSTE" => "IN",
            _ => throw new ArgumentException("TipoMovimiento inválido. Usa ENTRADA | SALIDA | AJUSTE (o IN | OUT).")
        };
    }
}
