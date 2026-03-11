using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class InventarioRepuestoRepository : IInventarioRepuestoRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    // ✅ BD FINAL
    private const string SP_STOCK_GET = "inventario.usp_Stock_Get";
    private const string SP_STOCK_MOV = "inventario.usp_Stock_AplicarMovimiento";

    public InventarioRepuestoRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    private static object DbOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    public async Task<List<StockProductoResponse>> ListarStockAsync(string? q)
    {
        using var cn = _cnFactory.CreateConnection();

        // Defensa: evita ejecutar comandos con conexión cerrada.
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        // ✅ BD FINAL: no existe usp_StockProducto_List, se arma con join Producto + Stock
        var sql = @"
SELECT
    p.IdProducto,
    p.Nombre,
    p.TipoProducto,
    ISNULL(p.Codigo,'') AS Codigo,
    p.Precio,
    CAST(ISNULL(s.CantidadActual, 0) AS DECIMAL(18,2)) AS Stock,
    s.Referencia,
    s.FechaActualizacion
FROM inventario.Producto p
LEFT JOIN inventario.Stock s ON s.IdProducto = p.IdProducto
WHERE
    (@Q IS NULL OR @Q = '' OR p.Nombre LIKE '%' + @Q + '%' OR p.Codigo LIKE '%' + @Q + '%')
ORDER BY p.IdProducto DESC;";

        using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
        cmd.Parameters.AddWithValue("@Q", DbOrNull(q));

        var list = new List<StockProductoResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

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
        using var cn = _cnFactory.CreateConnection();

        // ✅ BD FINAL: armamos Producto + Stock con join (más estable que depender solo del SP)
        var sql = @"
SELECT
    p.IdProducto,
    p.Nombre,
    p.TipoProducto,
    ISNULL(p.Codigo,'') AS Codigo,
    p.Precio,
    CAST(ISNULL(s.CantidadActual, 0) AS DECIMAL(18,2)) AS Stock,
    s.Referencia,
    s.FechaActualizacion
FROM inventario.Producto p
LEFT JOIN inventario.Stock s ON s.IdProducto = p.IdProducto
WHERE p.IdProducto = @IdProducto;";

        using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
        cmd.Parameters.AddWithValue("@IdProducto", idProducto);

        using var rd = await cmd.ExecuteReaderAsync();
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
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_STOCK_MOV, cn) { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdProducto", req.IdProducto);
        cmd.Parameters.AddWithValue("@TipoMovimiento", MapTipo(req.TipoMovimiento)); // BD final espera IN/OUT (CHAR(3))
        cmd.Parameters.AddWithValue("@Cantidad", req.Cantidad);
        cmd.Parameters.AddWithValue("@Referencia", DbOrNull(req.Referencia));
        cmd.Parameters.AddWithValue("@Observacion", req.TipoMovimiento.Trim().ToUpperInvariant() == "AJUSTE" ? "AJUSTE" : DBNull.Value);
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