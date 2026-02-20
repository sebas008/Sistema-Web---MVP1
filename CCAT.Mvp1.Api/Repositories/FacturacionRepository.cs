using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class FacturacionRepository : IFacturacionRepository
{
    private readonly IDbConnectionFactory _factory;
    public FacturacionRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> EmitirAsync(FacturaEmitirRequest req)
    {
        using var cn = _factory.CreateConnection(); // normalmente devuelve SqlConnection como IDbConnection
        using var cmd = new SqlCommand("contabilidad.usp_Factura_Emitir", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdCliente", req.IdCliente);
        cmd.Parameters.AddWithValue("@Usuario", req.Usuario);

        // Si tu SP acepta fecha
        cmd.Parameters.AddWithValue("@Fecha", (object?)req.Fecha ?? DBNull.Value);

        // Si tu SP acepta detalle como json/csv, lo agregas aquí
        // cmd.Parameters.AddWithValue("@DetalleJson", (object?)req.DetalleJson ?? DBNull.Value);

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<FacturaResponse?> ObtenerPorIdAsync(int idFactura)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("contabilidad.usp_Factura_Get", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdFactura", idFactura);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        // ⚠️ Ajusta nombres de columnas si tu SP devuelve otras (Numero/Referencia/Fecha/Total/Estado)
        return new FacturaResponse
        {
            IdFactura = SafeGetInt(rd, "IdFactura"),
            Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? "",
            Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
            Total = SafeGetDecimal(rd, "Total") ?? 0m,
            Estado = SafeGetString(rd, "Estado") ?? "EMITIDA",
            Cliente = SafeGetString(rd, "Cliente")
        };
    }

    public async Task<List<FacturaResponse>> ListarAsync(string? q)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("contabilidad.usp_Factura_List", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Q", (object?)q ?? DBNull.Value);

        var list = new List<FacturaResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new FacturaResponse
            {
                IdFactura = SafeGetInt(rd, "IdFactura"),
                Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? "",
                Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                Total = SafeGetDecimal(rd, "Total") ?? 0m,
                Estado = SafeGetString(rd, "Estado") ?? "",
                Cliente = SafeGetString(rd, "Cliente")
            });
        }

        return list;
    }

    // Helpers para no reventar si el SP cambia columnas
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