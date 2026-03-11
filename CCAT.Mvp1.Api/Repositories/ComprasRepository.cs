using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Contabilidad;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class ComprasRepository : IComprasRepository
{
    private readonly IDbConnectionFactory _factory;
    public ComprasRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> RegistrarAsync(CompraRegistrarRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Serie)) throw new ArgumentException("Serie obligatoria.");
        if (req.IdProveedor <= 0) throw new ArgumentException("IdProveedor inválido.");
        if (req.Detalle == null || req.Detalle.Count == 0) throw new ArgumentException("Detalle vacío.");

        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await ((SqlConnection)cn).OpenAsync();

        using var cmd = new SqlCommand("contabilidad.usp_Compra_Registrar", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Serie", req.Serie);
        cmd.Parameters.AddWithValue("@IdProveedor", req.IdProveedor);
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

    public async Task<CompraResponse?> ObtenerPorIdAsync(int idCompra)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await ((SqlConnection)cn).OpenAsync();

        var sql = @"
SELECT 
    c.IdCompra,
    CONCAT(c.Serie,'-',RIGHT('00000000'+CAST(c.Numero AS VARCHAR(20)),8)) AS Numero,
    CAST(c.FechaEmision AS DATETIME) AS Fecha,
    c.Total,
    c.Estado,
    p.RazonSocial AS Proveedor
FROM contabilidad.Compra c
INNER JOIN contabilidad.Proveedor p ON p.IdProveedor = c.IdProveedor
WHERE c.IdCompra = @IdCompra;";

        using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@IdCompra", idCompra);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new CompraResponse
        {
            IdCompra = SafeGetInt(rd, "IdCompra"),
            Numero = SafeGetString(rd, "Numero") ?? "",
            Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
            Total = SafeGetDecimal(rd, "Total") ?? 0m,
            Estado = SafeGetString(rd, "Estado") ?? "REGISTRADA",
            Proveedor = SafeGetString(rd, "Proveedor")
        };
    }

    public async Task<List<CompraResponse>> ListarAsync(string? q)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await ((SqlConnection)cn).OpenAsync();

        var sql = @"
SELECT TOP (200)
    c.IdCompra,
    CONCAT(c.Serie,'-',RIGHT('00000000'+CAST(c.Numero AS VARCHAR(20)),8)) AS Numero,
    CAST(c.FechaEmision AS DATETIME) AS Fecha,
    c.Total,
    c.Estado,
    p.RazonSocial AS Proveedor
FROM contabilidad.Compra c
INNER JOIN contabilidad.Proveedor p ON p.IdProveedor = c.IdProveedor
WHERE (@Q IS NULL 
       OR CONCAT(c.Serie,'-',RIGHT('00000000'+CAST(c.Numero AS VARCHAR(20)),8)) LIKE '%' + @Q + '%'
       OR p.RazonSocial LIKE '%' + @Q + '%')
ORDER BY c.IdCompra DESC;";

        using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@Q", (object?)(string.IsNullOrWhiteSpace(q) ? DBNull.Value : q));

        var list = new List<CompraResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new CompraResponse
            {
                IdCompra = SafeGetInt(rd, "IdCompra"),
                Numero = SafeGetString(rd, "Numero") ?? "",
                Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                Total = SafeGetDecimal(rd, "Total") ?? 0m,
                Estado = SafeGetString(rd, "Estado") ?? "",
                Proveedor = SafeGetString(rd, "Proveedor")
            });
        }

        return list;
    }

    public async Task<bool> AnularAsync(int idCompra)
    {
        using var cn = _factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await ((SqlConnection)cn).OpenAsync();

        const string sql = @"
UPDATE contabilidad.Compra
SET Estado = 'ANULADA'
WHERE IdCompra = @IdCompra
  AND ISNULL(Estado, '') <> 'ANULADA';";

        using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@IdCompra", idCompra);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private static DataTable BuildDetalleTvp(List<DetalleItemDto> detalle)
    {
        // Debe coincidir exactamente con contabilidad.TVP_DetalleItem (7 columnas)
        var dt = new DataTable();
        dt.Columns.Add("Item", typeof(int));
        dt.Columns.Add("TipoItem", typeof(string));
        dt.Columns.Add("IdProducto", typeof(int));
        dt.Columns.Add("IdVehiculo", typeof(int));
        dt.Columns.Add("Descripcion", typeof(string));
        dt.Columns.Add("Cantidad", typeof(decimal));
        dt.Columns.Add("PrecioUnitario", typeof(decimal));

        for (int i = 0; i < detalle.Count; i++)
        {
            var it = detalle[i];
            var row = dt.NewRow();
            row["Item"] = i + 1;
            row["TipoItem"] = it.IdProducto.HasValue ? "PRODUCTO" : "SERVICIO";
            row["IdProducto"] = (object?)it.IdProducto ?? DBNull.Value;
            row["IdVehiculo"] = DBNull.Value;
            row["Descripcion"] = string.IsNullOrWhiteSpace(it.Descripcion) ? DBNull.Value : it.Descripcion;
            row["Cantidad"] = it.Cantidad;
            row["PrecioUnitario"] = it.PrecioUnitario;
            dt.Rows.Add(row);
        }
        return dt;
    }

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
