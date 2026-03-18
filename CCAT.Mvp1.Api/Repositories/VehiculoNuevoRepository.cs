using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.VehiculosNuevos;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class VehiculoNuevoRepository : IVehiculoNuevoRepository
{
    private readonly IDbConnectionFactory _cnFactory;

    private const string SP_UPSERT = "vehiculos.usp_Vehiculo_Upsert";
    private const string SP_STOCK_MOV = "vehiculos.usp_VehiculoStock_AplicarMovimiento";

    public VehiculoNuevoRepository(IDbConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    private static object DbOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static object DbOrNull(DateTime? value)
        => value.HasValue ? value.Value.Date : DBNull.Value;

    private static object DbOrNull(int? value)
        => value.HasValue ? value.Value : DBNull.Value;

    private static object DbOrNull(decimal? value)
        => value.HasValue ? value.Value : DBNull.Value;

    private static object DbOrNull(bool? value)
        => value.HasValue ? value.Value : DBNull.Value;

    public async Task<List<VehiculoNuevoResponse>> ListarAsync(string? q, bool? activo)
    {
        using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

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
    CAST(ISNULL(vs.CantidadActual,0) AS DECIMAL(18,2)) AS StockActual,
    v.CodigoVehiculo,
    v.CodigoExterno,
    v.ModeloLegal,
    v.TipoVehiculo,
    CAST(v.PrecioCompra AS DECIMAL(18,2)) AS PrecioCompra,
    CAST(v.PrecioVenta AS DECIMAL(18,2)) AS PrecioVenta,
    v.TipoTransmision,
    v.ColorExterior,
    v.ColorInterior,
    v.NumeroAsientos,
    v.NumeroPuertas,
    v.CilindrajeCc,
    v.PotenciaHp,
    v.TipoCombustible,
    v.NumeroMotor,
    v.NumeroChasis,
    v.ModeloTecnico,
    v.CodigoSap,
    CAST(v.PesoBruto AS DECIMAL(18,2)) AS PesoBruto,
    CAST(v.CargaUtil AS DECIMAL(18,2)) AS CargaUtil,
    v.EstadoVehiculo,
    v.Ubicacion,
    v.SeccionAsignada,
    CAST(v.FechaIngreso AS DATE) AS FechaIngreso,
    v.Catalitico,
    v.TipoCatalitico,
    CAST(v.BonoUsd AS DECIMAL(18,2)) AS BonoUsd,
    v.Pagado,
    v.TestDrive,
    v.UnidadTestDrive,
    v.Km0,
    v.Observacion
FROM vehiculos.Vehiculo v
LEFT JOIN vehiculos.VehiculoStock vs ON vs.IdVehiculo = v.IdVehiculo
WHERE
    (@Activo IS NULL OR v.Activo = @Activo)
    AND (
        @Q IS NULL OR @Q = ''
        OR v.Marca LIKE '%' + @Q + '%'
        OR v.Modelo LIKE '%' + @Q + '%'
        OR v.VIN LIKE '%' + @Q + '%'
        OR v.CodigoVehiculo LIKE '%' + @Q + '%'
        OR v.CodigoExterno LIKE '%' + @Q + '%'
        OR v.NumeroChasis LIKE '%' + @Q + '%'
        OR v.NumeroMotor LIKE '%' + @Q + '%'
    )
ORDER BY v.IdVehiculo DESC;";

        using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
        cmd.Parameters.AddWithValue("@Q", DbOrNull(q));
        cmd.Parameters.AddWithValue("@Activo", (object?)activo ?? DBNull.Value);

        var list = new List<VehiculoNuevoResponse>();
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
            list.Add(MapVehiculo(rd));

        return list;
    }

    public async Task<VehiculoNuevoResponse?> ObtenerPorIdAsync(int idVehiculo)
    {
        using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

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
    CAST(ISNULL(vs.CantidadActual,0) AS DECIMAL(18,2)) AS StockActual,
    v.CodigoVehiculo,
    v.CodigoExterno,
    v.ModeloLegal,
    v.TipoVehiculo,
    CAST(v.PrecioCompra AS DECIMAL(18,2)) AS PrecioCompra,
    CAST(v.PrecioVenta AS DECIMAL(18,2)) AS PrecioVenta,
    v.TipoTransmision,
    v.ColorExterior,
    v.ColorInterior,
    v.NumeroAsientos,
    v.NumeroPuertas,
    v.CilindrajeCc,
    v.PotenciaHp,
    v.TipoCombustible,
    v.NumeroMotor,
    v.NumeroChasis,
    v.ModeloTecnico,
    v.CodigoSap,
    CAST(v.PesoBruto AS DECIMAL(18,2)) AS PesoBruto,
    CAST(v.CargaUtil AS DECIMAL(18,2)) AS CargaUtil,
    v.EstadoVehiculo,
    v.Ubicacion,
    v.SeccionAsignada,
    CAST(v.FechaIngreso AS DATE) AS FechaIngreso,
    v.Catalitico,
    v.TipoCatalitico,
    CAST(v.BonoUsd AS DECIMAL(18,2)) AS BonoUsd,
    v.Pagado,
    v.TestDrive,
    v.UnidadTestDrive,
    v.Km0,
    v.Observacion
FROM vehiculos.Vehiculo v
LEFT JOIN vehiculos.VehiculoStock vs ON vs.IdVehiculo = v.IdVehiculo
WHERE v.IdVehiculo = @IdVehiculo;";

        using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
        cmd.Parameters.AddWithValue("@IdVehiculo", idVehiculo);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return MapVehiculo(rd);
    }

    public async Task<int> CrearAsync(VehiculoNuevoCrearRequest req)
    {
        using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        using var cmd = new SqlCommand(SP_UPSERT, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@IdVehiculo", DBNull.Value);
        AddUpsertParameters(cmd, req, req.Activo);

        var idObj = await cmd.ExecuteScalarAsync();
        var idVehiculo = Convert.ToInt32(idObj);

        await EnsureInitialStockAsync(cn, idVehiculo, req.Usuario);
        return idVehiculo;
    }

    public async Task<int> ActualizarAsync(int idVehiculo, VehiculoNuevoActualizarRequest req)
    {
        var actual = await ObtenerPorIdAsync(idVehiculo);
        if (actual is null) throw new Exception("Vehículo no existe.");

        using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        using var cmd = new SqlCommand(SP_UPSERT, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@IdVehiculo", idVehiculo);
        AddUpsertParameters(cmd, req, actual.Activo);

        var idObj = await cmd.ExecuteScalarAsync();
        var idActualizado = Convert.ToInt32(idObj);

        await EnsureInitialStockAsync(cn, idActualizado, req.Usuario);
        return idActualizado;
    }

    public async Task<int> CambiarEstadoAsync(int idVehiculo, bool activo, string usuario)
    {
        var actual = await ObtenerPorIdAsync(idVehiculo);
        if (actual is null) throw new Exception("Vehículo no existe.");

        using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        using var cmd = new SqlCommand(SP_UPSERT, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@IdVehiculo", idVehiculo);

        AddUpsertParameters(cmd, new VehiculoNuevoCrearRequest
        {
            VIN = actual.VIN,
            Marca = actual.Marca,
            Modelo = actual.Modelo,
            Anio = actual.Anio,
            Version = actual.Version,
            Color = actual.Color,
            PrecioLista = actual.PrecioLista,
            Usuario = string.IsNullOrWhiteSpace(usuario) ? "admin" : usuario,
            CodigoVehiculo = actual.CodigoVehiculo,
            CodigoExterno = actual.CodigoExterno,
            ModeloLegal = actual.ModeloLegal,
            TipoVehiculo = actual.TipoVehiculo,
            PrecioCompra = actual.PrecioCompra,
            PrecioVenta = actual.PrecioVenta,
            TipoTransmision = actual.TipoTransmision,
            ColorExterior = actual.ColorExterior,
            ColorInterior = actual.ColorInterior,
            NumeroAsientos = actual.NumeroAsientos,
            NumeroPuertas = actual.NumeroPuertas,
            CilindrajeCc = actual.CilindrajeCc,
            PotenciaHp = actual.PotenciaHp,
            TipoCombustible = actual.TipoCombustible,
            NumeroMotor = actual.NumeroMotor,
            NumeroChasis = actual.NumeroChasis,
            ModeloTecnico = actual.ModeloTecnico,
            CodigoSap = actual.CodigoSap,
            PesoBruto = actual.PesoBruto,
            CargaUtil = actual.CargaUtil,
            EstadoVehiculo = actual.EstadoVehiculo,
            Ubicacion = actual.Ubicacion,
            SeccionAsignada = actual.SeccionAsignada,
            FechaIngreso = actual.FechaIngreso,
            Catalitico = actual.Catalitico,
            TipoCatalitico = actual.TipoCatalitico,
            BonoUsd = actual.BonoUsd,
            Pagado = actual.Pagado,
            TestDrive = actual.TestDrive,
            UnidadTestDrive = actual.UnidadTestDrive,
            Km0 = actual.Km0,
            Observacion = actual.Observacion,
        }, activo);

        var idObj = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(idObj);
    }

    public async Task AplicarMovimientoStockAsync(VehiculoNuevoStockMovimientoRequest req)
    {
        using var cn = _cnFactory.CreateConnection();
        if (cn.State != ConnectionState.Open) await cn.OpenAsync();

        using var cmd = new SqlCommand(SP_STOCK_MOV, cn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@IdVehiculo", req.IdVehiculo);
        cmd.Parameters.AddWithValue("@TipoMovimiento", MapTipo(req.TipoMovimiento));
        cmd.Parameters.AddWithValue("@Cantidad", (int)req.Cantidad);
        cmd.Parameters.AddWithValue("@Referencia", DbOrNull(req.Referencia));
        cmd.Parameters.AddWithValue("@Observacion", req.TipoMovimiento.Trim().ToUpperInvariant() == "AJUSTE" ? "AJUSTE" : DBNull.Value);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        await cmd.ExecuteNonQueryAsync();
    }

    private static void AddUpsertParameters(SqlCommand cmd, VehiculoNuevoCrearRequest req, bool activo)
    {
        cmd.Parameters.AddWithValue("@VIN", DbOrNull(req.VIN));
        cmd.Parameters.AddWithValue("@Marca", DbOrNull(req.Marca));
        cmd.Parameters.AddWithValue("@Modelo", DbOrNull(req.Modelo));
        cmd.Parameters.AddWithValue("@Anio", DbOrNull(req.Anio));
        cmd.Parameters.AddWithValue("@Version", DbOrNull(req.Version));
        cmd.Parameters.AddWithValue("@Color", DbOrNull(req.Color));
        cmd.Parameters.AddWithValue("@PrecioLista", DbOrNull(req.PrecioLista));
        cmd.Parameters.AddWithValue("@Activo", activo);
        cmd.Parameters.AddWithValue("@Usuario", DbOrNull(req.Usuario));

        cmd.Parameters.AddWithValue("@CodigoVehiculo", DbOrNull(req.CodigoVehiculo));
        cmd.Parameters.AddWithValue("@CodigoExterno", DbOrNull(req.CodigoExterno));
        cmd.Parameters.AddWithValue("@ModeloLegal", DbOrNull(req.ModeloLegal));
        cmd.Parameters.AddWithValue("@TipoVehiculo", DbOrNull(req.TipoVehiculo));
        cmd.Parameters.AddWithValue("@PrecioCompra", DbOrNull(req.PrecioCompra));
        cmd.Parameters.AddWithValue("@PrecioVenta", DbOrNull(req.PrecioVenta));
        cmd.Parameters.AddWithValue("@TipoTransmision", DbOrNull(req.TipoTransmision));
        cmd.Parameters.AddWithValue("@ColorExterior", DbOrNull(req.ColorExterior));
        cmd.Parameters.AddWithValue("@ColorInterior", DbOrNull(req.ColorInterior));
        cmd.Parameters.AddWithValue("@NumeroAsientos", DbOrNull(req.NumeroAsientos));
        cmd.Parameters.AddWithValue("@NumeroPuertas", DbOrNull(req.NumeroPuertas));
        cmd.Parameters.AddWithValue("@CilindrajeCc", DbOrNull(req.CilindrajeCc));
        cmd.Parameters.AddWithValue("@PotenciaHp", DbOrNull(req.PotenciaHp));
        cmd.Parameters.AddWithValue("@TipoCombustible", DbOrNull(req.TipoCombustible));
        cmd.Parameters.AddWithValue("@NumeroMotor", DbOrNull(req.NumeroMotor));
        cmd.Parameters.AddWithValue("@NumeroChasis", DbOrNull(req.NumeroChasis));
        cmd.Parameters.AddWithValue("@ModeloTecnico", DbOrNull(req.ModeloTecnico));
        cmd.Parameters.AddWithValue("@CodigoSap", DbOrNull(req.CodigoSap));
        cmd.Parameters.AddWithValue("@PesoBruto", DbOrNull(req.PesoBruto));
        cmd.Parameters.AddWithValue("@CargaUtil", DbOrNull(req.CargaUtil));
        cmd.Parameters.AddWithValue("@EstadoVehiculo", DbOrNull(req.EstadoVehiculo));
        cmd.Parameters.AddWithValue("@Ubicacion", DbOrNull(req.Ubicacion));
        cmd.Parameters.AddWithValue("@SeccionAsignada", DbOrNull(req.SeccionAsignada));
        cmd.Parameters.AddWithValue("@FechaIngreso", DbOrNull(req.FechaIngreso));
        cmd.Parameters.AddWithValue("@Catalitico", DbOrNull(req.Catalitico));
        cmd.Parameters.AddWithValue("@TipoCatalitico", DbOrNull(req.TipoCatalitico));
        cmd.Parameters.AddWithValue("@BonoUsd", DbOrNull(req.BonoUsd));
        cmd.Parameters.AddWithValue("@Pagado", DbOrNull(req.Pagado));
        cmd.Parameters.AddWithValue("@TestDrive", DbOrNull(req.TestDrive));
        cmd.Parameters.AddWithValue("@UnidadTestDrive", DbOrNull(req.UnidadTestDrive));
        cmd.Parameters.AddWithValue("@Km0", DbOrNull(req.Km0));
        cmd.Parameters.AddWithValue("@Observacion", DbOrNull(req.Observacion));
    }

    private static async Task EnsureInitialStockAsync(SqlConnection cn, int idVehiculo, string? usuario)
    {
        var user = string.IsNullOrWhiteSpace(usuario) ? "admin" : usuario.Trim();
        var sql = @"
DECLARE @IdVehiculo INT = @pIdVehiculo;
DECLARE @Usuario NVARCHAR(50) = @pUsuario;

IF EXISTS (SELECT 1 FROM vehiculos.VehiculoStock WHERE IdVehiculo = @IdVehiculo)
BEGIN
    UPDATE vehiculos.VehiculoStock
    SET
        CantidadActual = CASE WHEN ISNULL(CantidadActual, 0) <= 0 THEN 1 ELSE CantidadActual END,
        UsuarioActualizacion = CASE WHEN COL_LENGTH('vehiculos.VehiculoStock', 'UsuarioActualizacion') IS NOT NULL THEN ISNULL(UsuarioActualizacion, @Usuario) ELSE UsuarioActualizacion END,
        FechaActualizacion = CASE WHEN COL_LENGTH('vehiculos.VehiculoStock', 'FechaActualizacion') IS NOT NULL THEN ISNULL(FechaActualizacion, GETDATE()) ELSE FechaActualizacion END,
        UsuarioModificacion = CASE WHEN COL_LENGTH('vehiculos.VehiculoStock', 'UsuarioModificacion') IS NOT NULL THEN ISNULL(UsuarioModificacion, @Usuario) ELSE UsuarioModificacion END,
        FechaModificacion = CASE WHEN COL_LENGTH('vehiculos.VehiculoStock', 'FechaModificacion') IS NOT NULL THEN ISNULL(FechaModificacion, GETDATE()) ELSE FechaModificacion END
    WHERE IdVehiculo = @IdVehiculo;
END
ELSE
BEGIN
    DECLARE @cols NVARCHAR(MAX) = N'IdVehiculo, CantidadActual';
    DECLARE @vals NVARCHAR(MAX) = N'@IdVehiculo, 1';

    IF COL_LENGTH('vehiculos.VehiculoStock', 'UsuarioCreacion') IS NOT NULL
    BEGIN
        SET @cols += N', UsuarioCreacion';
        SET @vals += N', @Usuario';
    END

    IF COL_LENGTH('vehiculos.VehiculoStock', 'FechaCreacion') IS NOT NULL
    BEGIN
        SET @cols += N', FechaCreacion';
        SET @vals += N', GETDATE()';
    END

    IF COL_LENGTH('vehiculos.VehiculoStock', 'UsuarioActualizacion') IS NOT NULL
    BEGIN
        SET @cols += N', UsuarioActualizacion';
        SET @vals += N', @Usuario';
    END

    IF COL_LENGTH('vehiculos.VehiculoStock', 'FechaActualizacion') IS NOT NULL
    BEGIN
        SET @cols += N', FechaActualizacion';
        SET @vals += N', GETDATE()';
    END

    IF COL_LENGTH('vehiculos.VehiculoStock', 'UsuarioModificacion') IS NOT NULL
    BEGIN
        SET @cols += N', UsuarioModificacion';
        SET @vals += N', @Usuario';
    END

    IF COL_LENGTH('vehiculos.VehiculoStock', 'FechaModificacion') IS NOT NULL
    BEGIN
        SET @cols += N', FechaModificacion';
        SET @vals += N', GETDATE()';
    END

    DECLARE @sql NVARCHAR(MAX) = N'INSERT INTO vehiculos.VehiculoStock (' + @cols + N') VALUES (' + @vals + N');';
    EXEC sp_executesql @sql, N'@IdVehiculo INT, @Usuario NVARCHAR(50)', @IdVehiculo, @Usuario;
END;";

        using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
        cmd.Parameters.AddWithValue("@pIdVehiculo", idVehiculo);
        cmd.Parameters.AddWithValue("@pUsuario", user);
        await cmd.ExecuteNonQueryAsync();
    }

    private static VehiculoNuevoResponse MapVehiculo(SqlDataReader rd)
    {
        return new VehiculoNuevoResponse
        {
            IdVehiculo = rd.GetInt32(rd.GetOrdinal("IdVehiculo")),
            VIN = ReadString(rd, "VIN"),
            Marca = ReadString(rd, "Marca") ?? "",
            Modelo = ReadString(rd, "Modelo") ?? "",
            Anio = ReadInt(rd, "Anio") ?? 0,
            Version = ReadString(rd, "Version"),
            Color = ReadString(rd, "Color"),
            PrecioLista = ReadDecimal(rd, "PrecioLista") ?? 0,
            Activo = ReadBool(rd, "Activo") ?? false,
            StockActual = ReadDecimal(rd, "StockActual") ?? 0,
            CodigoVehiculo = ReadString(rd, "CodigoVehiculo"),
            CodigoExterno = ReadString(rd, "CodigoExterno"),
            ModeloLegal = ReadString(rd, "ModeloLegal"),
            TipoVehiculo = ReadString(rd, "TipoVehiculo"),
            PrecioCompra = ReadDecimal(rd, "PrecioCompra"),
            PrecioVenta = ReadDecimal(rd, "PrecioVenta"),
            TipoTransmision = ReadString(rd, "TipoTransmision"),
            ColorExterior = ReadString(rd, "ColorExterior"),
            ColorInterior = ReadString(rd, "ColorInterior"),
            NumeroAsientos = ReadInt(rd, "NumeroAsientos"),
            NumeroPuertas = ReadInt(rd, "NumeroPuertas"),
            CilindrajeCc = ReadString(rd, "CilindrajeCc"),
            PotenciaHp = ReadString(rd, "PotenciaHp"),
            TipoCombustible = ReadString(rd, "TipoCombustible"),
            NumeroMotor = ReadString(rd, "NumeroMotor"),
            NumeroChasis = ReadString(rd, "NumeroChasis"),
            ModeloTecnico = ReadString(rd, "ModeloTecnico"),
            CodigoSap = ReadString(rd, "CodigoSap"),
            PesoBruto = ReadDecimal(rd, "PesoBruto"),
            CargaUtil = ReadDecimal(rd, "CargaUtil"),
            EstadoVehiculo = ReadString(rd, "EstadoVehiculo"),
            Ubicacion = ReadString(rd, "Ubicacion"),
            SeccionAsignada = ReadString(rd, "SeccionAsignada"),
            FechaIngreso = ReadDate(rd, "FechaIngreso"),
            Catalitico = ReadBool(rd, "Catalitico"),
            TipoCatalitico = ReadString(rd, "TipoCatalitico"),
            BonoUsd = ReadDecimal(rd, "BonoUsd"),
            Pagado = ReadBool(rd, "Pagado"),
            TestDrive = ReadBool(rd, "TestDrive"),
            UnidadTestDrive = ReadString(rd, "UnidadTestDrive"),
            Km0 = ReadBool(rd, "Km0"),
            Observacion = ReadString(rd, "Observacion")
        };
    }

    private static string? ReadString(SqlDataReader rd, string col)
        => rd.IsDBNull(rd.GetOrdinal(col)) ? null : rd.GetString(rd.GetOrdinal(col));

    private static int? ReadInt(SqlDataReader rd, string col)
        => rd.IsDBNull(rd.GetOrdinal(col)) ? null : Convert.ToInt32(rd[col]);

    private static decimal? ReadDecimal(SqlDataReader rd, string col)
        => rd.IsDBNull(rd.GetOrdinal(col)) ? null : Convert.ToDecimal(rd[col]);

    private static bool? ReadBool(SqlDataReader rd, string col)
        => rd.IsDBNull(rd.GetOrdinal(col)) ? null : Convert.ToBoolean(rd[col]);

    private static DateTime? ReadDate(SqlDataReader rd, string col)
        => rd.IsDBNull(rd.GetOrdinal(col)) ? null : Convert.ToDateTime(rd[col]);

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
