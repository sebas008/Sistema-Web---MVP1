using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Contabilidad;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class FacturacionRepository : IFacturacionRepository
{
    private readonly IDbConnectionFactory _factory;
    public FacturacionRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> EmitirAsync(FacturaEmitirRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Serie)) throw new ArgumentException("Serie obligatoria.");
        if (req.IdCliente <= 0) throw new ArgumentException("IdCliente inválido.");
        if (req.Detalle == null || req.Detalle.Count == 0) throw new ArgumentException("Detalle vacío.");

        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("contabilidad.usp_Factura_Emitir", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Serie", req.Serie);
        cmd.Parameters.AddWithValue("@IdCliente", req.IdCliente);
        cmd.Parameters.AddWithValue("@FechaEmision", req.FechaEmision.Date);
        cmd.Parameters.AddWithValue("@Moneda", req.Moneda ?? "PEN");
        cmd.Parameters.AddWithValue("@AfectaStock", req.AfectaStock);
        cmd.Parameters.AddWithValue("@Usuario", req.Usuario ?? "admin");

        var tvp = BuildDetalleTvp(req.Detalle);
        var pDetalle = cmd.Parameters.AddWithValue("@Detalle", tvp);
        pDetalle.SqlDbType = SqlDbType.Structured;
        pDetalle.TypeName = "contabilidad.TVP_DetalleItem";

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<FacturaResponse?> ObtenerPorIdAsync(int idFactura)
    {
        using var cn = _factory.CreateConnection();

        // No existe SP Get/List en tu script; leemos directo de tablas (sin romper lo existente).
        var sql = @"
SELECT 
    f.IdFactura,
    CONCAT(f.Serie,'-',RIGHT('00000000'+CAST(f.Numero AS VARCHAR(20)),8)) AS Numero,
    CAST(f.FechaEmision AS DATETIME) AS Fecha,
    f.Total,
    f.Estado,
    c.RazonSocial AS Cliente
FROM contabilidad.Factura f
INNER JOIN contabilidad.Cliente c ON c.IdCliente = f.IdCliente
WHERE f.IdFactura = @IdFactura;";

        using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@IdFactura", idFactura);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new FacturaResponse
        {
            IdFactura = SafeGetInt(rd, "IdFactura"),
            Numero = SafeGetString(rd, "Numero") ?? "",
            Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
            Total = SafeGetDecimal(rd, "Total") ?? 0m,
            Estado = SafeGetString(rd, "Estado") ?? "EMITIDA",
            Cliente = SafeGetString(rd, "Cliente")
        };
    }

    public async Task<List<FacturaResponse>> ListarAsync(string? q)
    {
        using var cn = _factory.CreateConnection();

        var sql = @"
SELECT TOP (200)
    f.IdFactura,
    CONCAT(f.Serie,'-',RIGHT('00000000'+CAST(f.Numero AS VARCHAR(20)),8)) AS Numero,
    CAST(f.FechaEmision AS DATETIME) AS Fecha,
    f.Total,
    f.Estado,
    c.RazonSocial AS Cliente
FROM contabilidad.Factura f
INNER JOIN contabilidad.Cliente c ON c.IdCliente = f.IdCliente
WHERE (@Q IS NULL 
       OR CONCAT(f.Serie,'-',RIGHT('00000000'+CAST(f.Numero AS VARCHAR(20)),8)) LIKE '%' + @Q + '%'
       OR c.RazonSocial LIKE '%' + @Q + '%')
ORDER BY f.IdFactura DESC;";

        using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@Q", (object?)(string.IsNullOrWhiteSpace(q) ? DBNull.Value : q));

        var list = new List<FacturaResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new FacturaResponse
            {
                IdFactura = SafeGetInt(rd, "IdFactura"),
                Numero = SafeGetString(rd, "Numero") ?? "",
                Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                Total = SafeGetDecimal(rd, "Total") ?? 0m,
                Estado = SafeGetString(rd, "Estado") ?? "",
                Cliente = SafeGetString(rd, "Cliente")
            });
        }

        return list;
    }

    private static DataTable BuildDetalleTvp(List<DetalleItemDto> detalle)
    {
        // Debe calzar con contabilidad.TVP_DetalleItem (IdProducto, Descripcion, Cantidad, PrecioUnitario)
        var dt = new DataTable();
        dt.Columns.Add("IdProducto", typeof(int));
        dt.Columns.Add("Descripcion", typeof(string));
        dt.Columns.Add("Cantidad", typeof(decimal));
        dt.Columns.Add("PrecioUnitario", typeof(decimal));

        foreach (var it in detalle)
        {
            var row = dt.NewRow();
            row["IdProducto"] = (object?)it.IdProducto ?? DBNull.Value;
            row["Descripcion"] = (object?)it.Descripcion ?? DBNull.Value;
            row["Cantidad"] = it.Cantidad;
            row["PrecioUnitario"] = it.PrecioUnitario;
            dt.Rows.Add(row);
        }
        return dt;
    }

    // Helpers
    private static int SafeGetInt(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? rd.GetInt32(rd.GetOrdinal(col)) : 0;

    private static string? SafeGetString(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? rd.GetString(rd.GetOrdinal(col)) : null;

    private static DateTime? SafeGetDateTime(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? rd.GetDateTime(rd.GetOrdinal(col)) : null;

    private static decimal? SafeGetDecimal(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? rd.GetDecimal(rd.GetOrdinal(col)) : null;

    private static bool HasCol(SqlDataReader rd, string col)
    {
        for (int i = 0; i < rd.FieldCount; i++)
            if (string.Equals(rd.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
