using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.VehiculosNuevos;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class VehiculoNuevoRepository : IVehiculoNuevoRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    // ✅ Ajusta SOLO estos nombres si tus SPs se llaman distinto
    private const string SP_UPSERT = "inventario.usp_Vehiculo_Upsert";
    private const string SP_STOCK_MOV = "inventario.usp_VehiculoStock_AplicarMovimiento";

    // Recomendados (si no existen, abajo te dejo el SQL para crearlos)
    private const string SP_LIST = "inventario.usp_Vehiculo_List";
    private const string SP_GET = "inventario.usp_Vehiculo_Get";

    public VehiculoNuevoRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    private static object DbOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    public async Task<List<VehiculoNuevoResponse>> ListarAsync(string? q, bool? activo)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_LIST, cn) { CommandType = CommandType.StoredProcedure };

        cmd.Parameters.AddWithValue("@Q", DbOrNull(q));
        cmd.Parameters.AddWithValue("@Activo", (object?)activo ?? DBNull.Value);

        var list = new List<VehiculoNuevoResponse>();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(MapVehiculo(rd));
        }
        return list;
    }

    public async Task<VehiculoNuevoResponse?> ObtenerPorIdAsync(int idVehiculo)
    {
        using var cn = _cnFactory.CreateConnection();
        using var cmd = new SqlCommand(SP_GET, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@IdVehiculo", idVehiculo);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;
        return MapVehiculo(rd);
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
        cmd.Parameters.AddWithValue("@Activo", true); // si tu SP requiere Activo aquí, mantenlo; sino, ajustas
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task<int> CambiarEstadoAsync(int idVehiculo, bool activo, string usuario)
    {
        // Si tu SP Upsert también actualiza Activo, lo usamos.
        // (Si tienes un SP dedicado, cambia la llamada aquí.)
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
        cmd.Parameters.AddWithValue("@Cantidad", req.Cantidad);
        cmd.Parameters.AddWithValue("@TipoMovimiento", DbOrNull(req.TipoMovimiento));
        cmd.Parameters.AddWithValue("@Referencia", DbOrNull(req.Referencia));
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        await cmd.ExecuteNonQueryAsync();
    }

    private static VehiculoNuevoResponse MapVehiculo(SqlDataReader rd)
    {
        decimal stock = 0;
        if (HasColumn(rd, "StockActual") && !rd.IsDBNull(rd.GetOrdinal("StockActual")))
            stock = rd.GetDecimal(rd.GetOrdinal("StockActual"));

        return new VehiculoNuevoResponse
        {
            IdVehiculo = rd.GetInt32(rd.GetOrdinal("IdVehiculo")),
            VIN = HasColumn(rd, "VIN") && !rd.IsDBNull(rd.GetOrdinal("VIN")) ? rd.GetString(rd.GetOrdinal("VIN")) : null,
            Marca = rd.IsDBNull(rd.GetOrdinal("Marca")) ? "" : rd.GetString(rd.GetOrdinal("Marca")),
            Modelo = rd.IsDBNull(rd.GetOrdinal("Modelo")) ? "" : rd.GetString(rd.GetOrdinal("Modelo")),
            Anio = rd.GetInt32(rd.GetOrdinal("Anio")),
            Version = rd.IsDBNull(rd.GetOrdinal("Version")) ? "" : rd.GetString(rd.GetOrdinal("Version")),
            Color = rd.IsDBNull(rd.GetOrdinal("Color")) ? "" : rd.GetString(rd.GetOrdinal("Color")),
            PrecioLista = rd.GetDecimal(rd.GetOrdinal("PrecioLista")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
            StockActual = stock
        };
    }

    private static bool HasColumn(IDataRecord dr, string columnName)
    {
        for (int i = 0; i < dr.FieldCount; i++)
            if (dr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
