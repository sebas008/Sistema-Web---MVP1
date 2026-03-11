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

        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        using var cmd = new SqlCommand("contabilidad.usp_Factura_Emitir", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Serie", req.Serie);
        cmd.Parameters.AddWithValue("@IdCliente", req.IdCliente);
        cmd.Parameters.AddWithValue("@FechaEmision", req.FechaEmision.Date);
        cmd.Parameters.AddWithValue("@Moneda", string.IsNullOrWhiteSpace(req.Moneda) ? "PEN" : req.Moneda);
        cmd.Parameters.AddWithValue("@AfectaStock", req.AfectaStock);
        cmd.Parameters.AddWithValue("@Usuario", string.IsNullOrWhiteSpace(req.Usuario) ? "admin" : req.Usuario);

        var tvp = BuildDetalleTvp(req.Detalle);
        var pDetalle = cmd.Parameters.AddWithValue("@Detalle", tvp);
        pDetalle.SqlDbType = SqlDbType.Structured;
        pDetalle.TypeName = "contabilidad.TVP_DetalleItem";

        using var rd = await cmd.ExecuteReaderAsync();

        int? idFactura = null;
        if (await rd.ReadAsync())
        {
            if (HasCol(rd, "IdFactura") && !rd.IsDBNull(rd.GetOrdinal("IdFactura")))
                idFactura = Convert.ToInt32(rd["IdFactura"]);
            else if (!rd.IsDBNull(0))
                idFactura = Convert.ToInt32(rd.GetValue(0));
        }

        if (!idFactura.HasValue)
            throw new InvalidOperationException("El procedimiento de factura no devolvió IdFactura.");

        return idFactura.Value;
    }

    public async Task<FacturaResponse?> ObtenerPorIdAsync(int idFactura)
    {
        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        const string sqlCabecera = @"
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

        FacturaResponse? factura = null;
        using (var cmd = new SqlCommand(sqlCabecera, cn))
        {
            cmd.Parameters.AddWithValue("@IdFactura", idFactura);
            using var rd = await cmd.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                factura = new FacturaResponse
                {
                    IdFactura = SafeGetInt(rd, "IdFactura"),
                    Numero = SafeGetString(rd, "Numero") ?? string.Empty,
                    Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                    Total = SafeGetDecimal(rd, "Total") ?? 0m,
                    Estado = SafeGetString(rd, "Estado") ?? "EMITIDA",
                    Cliente = SafeGetString(rd, "Cliente"),
                    Detalle = new List<FacturaDetalleItemResponse>()
                };
            }
        }

        if (factura is null) return null;

        const string sqlDetalle = @"
SELECT 
    fd.Item,
    fd.TipoItem,
    fd.IdProducto,
    fd.IdVehiculo,
    fd.Descripcion,
    fd.Cantidad,
    fd.PrecioUnitario,
    fd.Importe
FROM contabilidad.FacturaDetalle fd
WHERE fd.IdFactura = @IdFactura
ORDER BY fd.Item;";

        using (var cmdDet = new SqlCommand(sqlDetalle, cn))
        {
            cmdDet.Parameters.AddWithValue("@IdFactura", idFactura);
            using var rdDet = await cmdDet.ExecuteReaderAsync();
            while (await rdDet.ReadAsync())
            {
                factura.Detalle!.Add(new FacturaDetalleItemResponse
                {
                    Item = SafeGetInt(rdDet, "Item"),
                    TipoItem = SafeGetString(rdDet, "TipoItem") ?? string.Empty,
                    IdProducto = SafeGetNullableInt(rdDet, "IdProducto"),
                    IdVehiculo = SafeGetNullableInt(rdDet, "IdVehiculo"),
                    Descripcion = SafeGetString(rdDet, "Descripcion") ?? string.Empty,
                    Cantidad = SafeGetDecimal(rdDet, "Cantidad") ?? 0m,
                    PrecioUnitario = SafeGetDecimal(rdDet, "PrecioUnitario") ?? 0m,
                    Importe = SafeGetDecimal(rdDet, "Importe") ?? 0m
                });
            }
        }

        return factura;
    }

    public async Task<List<FacturaResponse>> ListarAsync(string? q)
    {
        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        const string sql = @"
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

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Q", string.IsNullOrWhiteSpace(q) ? DBNull.Value : q);

        var list = new List<FacturaResponse>();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new FacturaResponse
            {
                IdFactura = SafeGetInt(rd, "IdFactura"),
                Numero = SafeGetString(rd, "Numero") ?? string.Empty,
                Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                Total = SafeGetDecimal(rd, "Total") ?? 0m,
                Estado = SafeGetString(rd, "Estado") ?? string.Empty,
                Cliente = SafeGetString(rd, "Cliente")
            });
        }

        return list;
    }

    public async Task AnularAsync(int idFactura)
    {
        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        const string sql = @"
UPDATE contabilidad.Factura
SET Estado = 'ANULADA'
WHERE IdFactura = @IdFactura AND Estado <> 'ANULADA';

IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM contabilidad.Factura WHERE IdFactura = @IdFactura)
    THROW 54104, 'La factura no existe.', 1;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdFactura", idFactura);
        await cmd.ExecuteNonQueryAsync();
    }

    private static DataTable BuildDetalleTvp(List<DetalleItemDto> detalle)
    {
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
            row["IdProducto"] = it.IdProducto.HasValue ? it.IdProducto.Value : DBNull.Value;
            row["IdVehiculo"] = DBNull.Value;
            row["Descripcion"] = string.IsNullOrWhiteSpace(it.Descripcion) ? string.Empty : it.Descripcion!;
            row["Cantidad"] = it.Cantidad;
            row["PrecioUnitario"] = it.PrecioUnitario;
            dt.Rows.Add(row);
        }

        return dt;
    }

    private static int SafeGetInt(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToInt32(rd[col]) : 0;

    private static int? SafeGetNullableInt(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToInt32(rd[col]) : null;

    private static string? SafeGetString(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToString(rd[col]) : null;

    private static DateTime? SafeGetDateTime(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToDateTime(rd[col]) : null;

    private static decimal? SafeGetDecimal(SqlDataReader rd, string col)
        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToDecimal(rd[col]) : null;

    private static bool HasCol(SqlDataReader rd, string col)
    {
        for (int i = 0; i < rd.FieldCount; i++)
        {
            if (string.Equals(rd.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
