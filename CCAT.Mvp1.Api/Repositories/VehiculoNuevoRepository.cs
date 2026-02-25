using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.VehiculosNuevos;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class VehiculoNuevoRepository : IVehiculoNuevoRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    // ✅ BD FINAL
    private const string SP_UPSERT = "vehiculos.usp_Vehiculo_Upsert";
    private const string SP_STOCK_MOV = "vehiculos.usp_VehiculoStock_AplicarMovimiento";

    public VehiculoNuevoRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    private static object DbOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    public async Task<List<VehiculoNuevoResponse>> ListarAsync(string? q, bool? activo)
    {
        using var cn = _cnFactory.CreateConnection();

        // ✅ BD FINAL: incluimos stock con join
        var sql = @"
SELECT
    v.IdVehiculo,
    v.VIN,
    v.Marca,
    v.Modelo,
    ISNULL(v.Anio,0) AS Anio,
    ISNULL(v.Version,'') AS Version,
    ISNULL(v.Color,'') AS Color,
    CAST(ISNULL(v.PrecioLista,0) AS DECIMAL(18,2)) AS PrecioLista,
    v.Activo,
    CAST(ISNULL(vs.CantidadActual,0) AS DECIMAL(18,2)) AS StockActual
FROM vehiculos.Vehiculo v
LEFT JOIN vehiculos.VehiculoStock vs ON vs.IdVehiculo = v.IdVehiculo
WHERE
    (@Activo IS NULL OR v.Activo = @Activo)
    AND (@Q IS NULL OR @Q = '' OR v.Marca LIKE '%' + @Q + '%' OR v.Modelo LIKE '%' + @Q + '%' OR v.VIN LIKE '%' + @Q + '%')
ORDER BY v.IdVehiculo DESC;";

        using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
        cmd.Parameters.AddWithValue("@Q", DbOrNull(q));
        cmd.Parameters.AddWithValue("@Activo", (object?)activo ?? DBNull.Value);

        var list = new List<VehiculoNuevoResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            list.Add(new VehiculoNuevoResponse
            {
                IdVehiculo = rd.GetInt32(rd.GetOrdinal("IdVehiculo")),
                VIN = rd.IsDBNull(rd.GetOrdinal("VIN")) ? null : rd.GetString(rd.GetOrdinal("VIN")),
                Marca = rd.IsDBNull(rd.GetOrdinal("Marca")) ? "" : rd.GetString(rd.GetOrdinal("Marca")),
                Modelo = rd.IsDBNull(rd.GetOrdinal("Modelo")) ? "" : rd.GetString(rd.GetOrdinal("Modelo")),
                Anio = rd.GetInt32(rd.GetOrdinal("Anio")),
                Version = rd.IsDBNull(rd.GetOrdinal("Version")) ? "" : rd.GetString(rd.GetOrdinal("Version")),
                Color = rd.IsDBNull(rd.GetOrdinal("Color")) ? "" : rd.GetString(rd.GetOrdinal("Color")),
                PrecioLista = rd.GetDecimal(rd.GetOrdinal("PrecioLista")),
                Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
                StockActual = rd.GetDecimal(rd.GetOrdinal("StockActual"))
            });
        }

        return list;
    }

    public async Task<VehiculoNuevoResponse?> ObtenerPorIdAsync(int idVehiculo)
    {
        using var cn = _cnFactory.CreateConnection();

        var sql = @"
SELECT
    v.IdVehiculo,
    v.VIN,
    v.Marca,
    v.Modelo,
    ISNULL(v.Anio,0) AS Anio,
    ISNULL(v.Version,'') AS Version,
    ISNULL(v.Color,'') AS Color,
    CAST(ISNULL(v.PrecioLista,0) AS DECIMAL(18,2)) AS PrecioLista,
    v.Activo,
    CAST(ISNULL(vs.CantidadActual,0) AS DECIMAL(18,2)) AS StockActual
FROM vehiculos.Vehiculo v
LEFT JOIN vehiculos.VehiculoStock vs ON vs.IdVehiculo = v.IdVehiculo
WHERE v.IdVehiculo = @IdVehiculo;";

        using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
        cmd.Parameters.AddWithValue("@IdVehiculo", idVehiculo);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new VehiculoNuevoResponse
        {
            IdVehiculo = rd.GetInt32(rd.GetOrdinal("IdVehiculo")),
            VIN = rd.IsDBNull(rd.GetOrdinal("VIN")) ? null : rd.GetString(rd.GetOrdinal("VIN")),
            Marca = rd.IsDBNull(rd.GetOrdinal("Marca")) ? "" : rd.GetString(rd.GetOrdinal("Marca")),
            Modelo = rd.IsDBNull(rd.GetOrdinal("Modelo")) ? "" : rd.GetString(rd.GetOrdinal("Modelo")),
            Anio = rd.GetInt32(rd.GetOrdinal("Anio")),
            Version = rd.IsDBNull(rd.GetOrdinal("Version")) ? "" : rd.GetString(rd.GetOrdinal("Version")),
            Color = rd.IsDBNull(rd.GetOrdinal("Color")) ? "" : rd.GetString(rd.GetOrdinal("Color")),
            PrecioLista = rd.GetDecimal(rd.GetOrdinal("PrecioLista")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
            StockActual = rd.GetDecimal(rd.GetOrdinal("StockActual"))
        };
    }

    public async Task<int> CrearAsync(VehiculoNuevoCrearRequest req)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_UPSERT, cn) { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdVehiculo", DBNull.Value);
        cmd.Parameters.AddWithValue("@VIN", DbOrNull(req.VIN));
        cmd.Parameters.AddWithValue("@Marca", DbOrNull(req.Marca));
        cmd.Parameters.AddWithValue("@Modelo", DbOrNull(req.Modelo));
        cmd.Parameters.AddWithValue("@Anio", req.Anio);
        cmd.Parameters.AddWithValue("@Version", DbOrNull(req.Version));
        cmd.Parameters.AddWithValue("@Color", DbOrNull(req.Color));
        cmd.Parameters.AddWithValue("@PrecioLista", req.PrecioLista);
        cmd.Parameters.AddWithValue("@Activo", req.Activo);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<int> ActualizarAsync(int idVehiculo, VehiculoNuevoActualizarRequest req)
    {
        // mantenemos Activo actual (para no forzarlo a true)
        var actual = await ObtenerPorIdAsync(idVehiculo);
        if (actual is null) throw new Exception("Vehículo no existe.");

        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_UPSERT, cn) { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdVehiculo", idVehiculo);
        cmd.Parameters.AddWithValue("@VIN", DbOrNull(req.VIN));
        cmd.Parameters.AddWithValue("@Marca", DbOrNull(req.Marca));
        cmd.Parameters.AddWithValue("@Modelo", DbOrNull(req.Modelo));
        cmd.Parameters.AddWithValue("@Anio", req.Anio);
        cmd.Parameters.AddWithValue("@Version", DbOrNull(req.Version));
        cmd.Parameters.AddWithValue("@Color", DbOrNull(req.Color));
        cmd.Parameters.AddWithValue("@PrecioLista", req.PrecioLista);
        cmd.Parameters.AddWithValue("@Activo", actual.Activo);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<int> CambiarEstadoAsync(int idVehiculo, bool activo, string usuario)
    {
        var actual = await ObtenerPorIdAsync(idVehiculo);
        if (actual is null) throw new Exception("Vehículo no existe.");

        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_UPSERT, cn) { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdVehiculo", idVehiculo);
        cmd.Parameters.AddWithValue("@VIN", DbOrNull(actual.VIN));
        cmd.Parameters.AddWithValue("@Marca", DbOrNull(actual.Marca));
        cmd.Parameters.AddWithValue("@Modelo", DbOrNull(actual.Modelo));
        cmd.Parameters.AddWithValue("@Anio", actual.Anio);
        cmd.Parameters.AddWithValue("@Version", DbOrNull(actual.Version));
        cmd.Parameters.AddWithValue("@Color", DbOrNull(actual.Color));
        cmd.Parameters.AddWithValue("@PrecioLista", actual.PrecioLista);
        cmd.Parameters.AddWithValue("@Activo", activo);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(usuario));

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task AplicarMovimientoStockAsync(VehiculoNuevoStockMovimientoRequest req)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_STOCK_MOV, cn) { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@IdVehiculo", req.IdVehiculo);
        cmd.Parameters.AddWithValue("@TipoMovimiento", MapTipo(req.TipoMovimiento)); // BD final espera IN/OUT (CHAR(3))
        cmd.Parameters.AddWithValue("@Cantidad", (int)req.Cantidad); // SP pide INT
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