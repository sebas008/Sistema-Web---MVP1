//using System.Data;
//using Microsoft.Data.SqlClient;
//using CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;
//using CCAT.Mvp1.Api.Interfaces;

//namespace CCAT.Mvp1.Api.Repositories;

//public class GuiasRepository : IGuiasRepository
//{
//    private readonly IDbConnectionFactory _factory;
//    public GuiasRepository(IDbConnectionFactory factory) => _factory = factory;

//    public async Task<int> EmitirAsync(GuiaEmitirRequest req)
//    //{
//        using var cn = _factory.CreateConnection();
//        if (cn.State != ConnectionState.Open)
//            await ((SqlConnection)cn).OpenAsync();

//        using var cmd = new SqlCommand("contabilidad.usp_Guia_Emitir", (SqlConnection)cn)
//        {
//            CommandType = CommandType.StoredProcedure
//        };

//        cmd.Parameters.AddWithValue("@IdCliente", req.IdCliente);
//        cmd.Parameters.AddWithValue("@Usuario", string.IsNullOrWhiteSpace(req.Usuario) ? "admin" : req.Usuario);
//        cmd.Parameters.AddWithValue("@Fecha", (object?)req.Fecha ?? DBNull.Value);
//        cmd.Parameters.AddWithValue("@DetalleJson", (object?)req.DetalleJson ?? DBNull.Value);

//        var idObj = await cmd.ExecuteScalarAsync();
//        return Convert.ToInt32(idObj);
//    }

//    public async Task<GuiaResponse?> ObtenerPorIdAsync(int idGuia)
//    {
//        using var cn = _factory.CreateConnection();
//        if (cn.State != ConnectionState.Open)
//            await ((SqlConnection)cn).OpenAsync();

//        using var cmd = new SqlCommand("contabilidad.usp_Guia_Get", (SqlConnection)cn)
//        {
//            CommandType = CommandType.StoredProcedure
//        };

//        cmd.Parameters.AddWithValue("@IdGuia", idGuia);

//        using var rd = await cmd.ExecuteReaderAsync();
//        if (!await rd.ReadAsync()) return null;

//        return new GuiaResponse
//        {
//            IdGuia = SafeGetInt(rd, "IdGuia"),
//            Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? "",
//            Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
//            Estado = SafeGetString(rd, "Estado") ?? "EMITIDA",
//            Destino = SafeGetString(rd, "Destino") ?? SafeGetString(rd, "Cliente")
//        };
//    }

//    public async Task<List<GuiaResponse>> ListarAsync(string? q)
//    {
//        using var cn = _factory.CreateConnection();
//        if (cn.State != ConnectionState.Open)
//            await ((SqlConnection)cn).OpenAsync();

//        using var cmd = new SqlCommand("contabilidad.usp_Guia_List", (SqlConnection)cn)
//        {
//            CommandType = CommandType.StoredProcedure
//        };

//        cmd.Parameters.AddWithValue("@Q", string.IsNullOrWhiteSpace(q) ? DBNull.Value : (object)q);

//        var list = new List<GuiaResponse>();
//        using var rd = await cmd.ExecuteReaderAsync();

//        while (await rd.ReadAsync())
//        {
//            list.Add(new GuiaResponse
//            {
//                IdGuia = SafeGetInt(rd, "IdGuia"),
//                Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? "",
//                Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
//                Estado = SafeGetString(rd, "Estado") ?? "",
//                Destino = SafeGetString(rd, "Destino") ?? SafeGetString(rd, "Cliente")
//            });
//        }

//        return list;
//    }

//    public async Task<bool> AnularAsync(int idGuia)
//    {
//        using var cn = _factory.CreateConnection();
//        if (cn.State != ConnectionState.Open)
//            await ((SqlConnection)cn).OpenAsync();

//        const string sql = @"
//UPDATE contabilidad.Guia
//SET Estado = 'ANULADA'
//WHERE IdGuia = @IdGuia
//  AND ISNULL(Estado, '') <> 'ANULADA';";

//        using var cmd = new SqlCommand(sql, (SqlConnection)cn);
//        cmd.Parameters.AddWithValue("@IdGuia", idGuia);

//        var rows = await cmd.ExecuteNonQueryAsync();
//        return rows > 0;
//    }

//    private static int SafeGetInt(SqlDataReader rd, string col)
//        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToInt32(rd[col]) : 0;

//    private static string? SafeGetString(SqlDataReader rd, string col)
//        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToString(rd[col]) : null;

//    private static DateTime? SafeGetDateTime(SqlDataReader rd, string col)
//        => HasCol(rd, col) && !rd.IsDBNull(rd.GetOrdinal(col)) ? Convert.ToDateTime(rd[col]) : null;

//    private static bool HasCol(SqlDataReader rd, string col)
//    {
//        for (int i = 0; i < rd.FieldCount; i++)
//            if (string.Equals(rd.GetName(i), col, StringComparison.OrdinalIgnoreCase))
//                return true;
//        return false;
//    }
//}
