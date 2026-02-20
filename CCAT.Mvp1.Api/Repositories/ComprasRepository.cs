using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class ComprasRepository : IComprasRepository
{
    private readonly IDbConnectionFactory _factory;
    public ComprasRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> RegistrarAsync(CompraRegistrarRequest req)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("contabilidad.usp_Compra_Registrar", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        // ✅ Ajusta a tus parámetros reales si difieren
        cmd.Parameters.AddWithValue("@IdProveedor", req.IdProveedor);
        cmd.Parameters.AddWithValue("@Usuario", req.Usuario);
        cmd.Parameters.AddWithValue("@Fecha", (object?)req.Fecha ?? DBNull.Value);

        // cmd.Parameters.AddWithValue("@DetalleJson", (object?)req.DetalleJson ?? DBNull.Value);

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<CompraResponse?> ObtenerPorIdAsync(int idCompra)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("contabilidad.usp_Compra_Get", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IdCompra", idCompra);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new CompraResponse
        {
            IdCompra = SafeGetInt(rd, "IdCompra"),
            Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? "",
            Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
            Total = SafeGetDecimal(rd, "Total") ?? 0m,
            Estado = SafeGetString(rd, "Estado") ?? "REGISTRADA",
            Proveedor = SafeGetString(rd, "Proveedor")
        };
    }

    public async Task<List<CompraResponse>> ListarAsync(string? q)
    {
        using var cn = _factory.CreateConnection();
        using var cmd = new SqlCommand("contabilidad.usp_Compra_List", (SqlConnection)cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Q", (object?)q ?? DBNull.Value);

        var list = new List<CompraResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new CompraResponse
            {
                IdCompra = SafeGetInt(rd, "IdCompra"),
                Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? "",
                Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                Total = SafeGetDecimal(rd, "Total") ?? 0m,
                Estado = SafeGetString(rd, "Estado") ?? "",
                Proveedor = SafeGetString(rd, "Proveedor")
            });
        }

        return list;
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