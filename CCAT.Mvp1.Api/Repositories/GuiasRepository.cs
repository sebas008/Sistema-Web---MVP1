using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class GuiasRepository : IGuiasRepository
{
    private readonly IDbConnectionFactory _factory;

    public GuiasRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<int> EmitirAsync(GuiaEmitirRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Serie))
            req.Serie = "G001";

        if ((req.Detalle == null || req.Detalle.Count == 0) && string.IsNullOrWhiteSpace(req.DetalleJson))
            throw new ArgumentException("Debe enviar al menos un ítem en el detalle de la guía.");

        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await cn.OpenAsync();

        if (await ProcedureExistsAsync(cn, "contabilidad", "usp_Guia_Emitir"))
            return await EmitirConStoredProcedureAsync(cn, req);

        if (await TableExistsAsync(cn, "contabilidad", "Guia"))
            return await EmitirDirectoAsync(cn, req);

        throw new InvalidOperationException("No existe la configuración de guías en la base de datos: faltan contabilidad.usp_Guia_Emitir y contabilidad.Guia.");
    }

    public async Task<GuiaResponse?> ObtenerPorIdAsync(int idGuia)
    {
        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await cn.OpenAsync();

        if (await ProcedureExistsAsync(cn, "contabilidad", "usp_Guia_Get"))
            return await ObtenerPorIdConSpAsync(cn, idGuia);

        if (await TableExistsAsync(cn, "contabilidad", "Guia"))
            return await ObtenerPorIdDirectoAsync(cn, idGuia);

        return null;
    }

    public async Task<List<GuiaResponse>> ListarAsync(string? q)
    {
        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await cn.OpenAsync();

        if (await ProcedureExistsAsync(cn, "contabilidad", "usp_Guia_List"))
            return await ListarConSpAsync(cn, q);

        if (await TableExistsAsync(cn, "contabilidad", "Guia"))
            return await ListarDirectoAsync(cn, q);

        return new List<GuiaResponse>();
    }

    public async Task<bool> AnularAsync(int idGuia)
    {
        using var cn = (SqlConnection)_factory.CreateConnection();
        if (cn.State != ConnectionState.Open)
            await cn.OpenAsync();

        if (await ProcedureExistsAsync(cn, "contabilidad", "usp_Guia_Anular"))
        {
            using var cmdSp = new SqlCommand("contabilidad.usp_Guia_Anular", cn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmdSp.Parameters.AddWithValue("@IdGuia", idGuia);

            var result = await cmdSp.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                return true;

            return Convert.ToInt32(result) > 0;
        }

        if (!await TableExistsAsync(cn, "contabilidad", "Guia"))
            throw new InvalidOperationException("No existe la tabla contabilidad.Guia para anular registros.");

        var guiaCols = await GetColumnsAsync(cn, "contabilidad", "Guia");
        if (!guiaCols.Contains("Estado"))
            throw new InvalidOperationException("La tabla contabilidad.Guia no tiene la columna Estado.");

        var setParts = new List<string> { "Estado = 'ANULADA'" };
        if (guiaCols.Contains("FechaActualizacion")) setParts.Add("FechaActualizacion = SYSDATETIME()");
        if (guiaCols.Contains("UsuarioActualizacion")) setParts.Add("UsuarioActualizacion = 'admin'");
        if (guiaCols.Contains("FechaAnulacion")) setParts.Add("FechaAnulacion = SYSDATETIME()");
        if (guiaCols.Contains("UsuarioAnulacion")) setParts.Add("UsuarioAnulacion = 'admin'");

        var sql = $"UPDATE contabilidad.Guia SET {string.Join(", ", setParts)} WHERE IdGuia = @IdGuia AND ISNULL(Estado, '') <> 'ANULADA'";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@IdGuia", idGuia);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private async Task<int> EmitirConStoredProcedureAsync(SqlConnection cn, GuiaEmitirRequest req)
    {
        using var cmd = new SqlCommand("contabilidad.usp_Guia_Emitir", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@Serie", req.Serie ?? "G001");
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@FechaEmision", req.FechaOperacion);
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@Fecha", req.FechaOperacion);
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@Tipo", NullIfWhiteSpace(req.Tipo));
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@MotivoTraslado", NullIfWhiteSpace(req.MotivoTraslado));
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@PuntoPartida", NullIfWhiteSpace(req.PuntoPartida));
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@PuntoLlegada", NullIfWhiteSpace(req.PuntoLlegada));
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@AfectaStock", req.AfectaStock);
        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@Usuario", string.IsNullOrWhiteSpace(req.Usuario) ? "admin" : req.Usuario);

        if (req.IdCliente > 0)
            await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@IdCliente", req.IdCliente);

        var detalleJson = !string.IsNullOrWhiteSpace(req.DetalleJson)
            ? req.DetalleJson
            : JsonSerializer.Serialize(req.Detalle ?? new List<GuiaDetalleItemRequest>());

        await AddProcParameterIfExistsAsync(cn, cmd, "contabilidad", "usp_Guia_Emitir", "@DetalleJson", detalleJson);

        if (await ProcedureHasParameterAsync(cn, "contabilidad", "usp_Guia_Emitir", "@Detalle"))
        {
            var tvp = BuildDetalleTvp(req.Detalle ?? new List<GuiaDetalleItemRequest>());
            var pDetalle = cmd.Parameters.AddWithValue("@Detalle", tvp);
            pDetalle.SqlDbType = SqlDbType.Structured;
            pDetalle.TypeName = "contabilidad.TVP_DetalleItem";
        }

        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException("El procedimiento de guías no devolvió IdGuia.");

        return Convert.ToInt32(result);
    }

    private async Task<int> EmitirDirectoAsync(SqlConnection cn, GuiaEmitirRequest req)
    {
        var guiaCols = await GetColumnsAsync(cn, "contabilidad", "Guia");
        var cols = new List<string>();
        var vals = new List<string>();
        using var cmd = new SqlCommand();
        cmd.Connection = cn;

        void Add(string column, string parameter, object? value)
        {
            if (!guiaCols.Contains(column)) return;
            cols.Add(column);
            vals.Add(parameter);
            cmd.Parameters.AddWithValue(parameter, value ?? DBNull.Value);
        }

        Add("Serie", "@Serie", req.Serie ?? "G001");
        Add("FechaEmision", "@FechaEmision", req.FechaOperacion);
        Add("Fecha", "@Fecha", req.FechaOperacion);
        Add("Tipo", "@Tipo", NullIfWhiteSpace(req.Tipo));
        Add("MotivoTraslado", "@MotivoTraslado", NullIfWhiteSpace(req.MotivoTraslado));
        Add("Motivo", "@Motivo", NullIfWhiteSpace(req.MotivoTraslado));
        Add("PuntoPartida", "@PuntoPartida", NullIfWhiteSpace(req.PuntoPartida));
        Add("PuntoLlegada", "@PuntoLlegada", NullIfWhiteSpace(req.PuntoLlegada));
        Add("AfectaStock", "@AfectaStock", req.AfectaStock);
        Add("Estado", "@Estado", "EMITIDA");
        Add("Usuario", "@Usuario", string.IsNullOrWhiteSpace(req.Usuario) ? "admin" : req.Usuario);
        Add("UsuarioCreacion", "@UsuarioCreacion", string.IsNullOrWhiteSpace(req.Usuario) ? "admin" : req.Usuario);
        Add("FechaCreacion", "@FechaCreacion", DateTime.Now);
        if (req.IdCliente > 0) Add("IdCliente", "@IdCliente", req.IdCliente);

        if (cols.Count == 0)
            throw new InvalidOperationException("La tabla contabilidad.Guia no tiene columnas compatibles para registrar la guía.");

        if (guiaCols.Contains("Numero") && !cols.Contains("Numero"))
        {
            cols.Add("Numero");
            vals.Add("(SELECT ISNULL(MAX(CAST(Numero AS INT)), 0) + 1 FROM contabilidad.Guia)");
        }

        cmd.CommandText = $"INSERT INTO contabilidad.Guia ({string.Join(", ", cols)}) OUTPUT INSERTED.IdGuia VALUES ({string.Join(", ", vals)});";
        var idObj = await cmd.ExecuteScalarAsync();
        var idGuia = Convert.ToInt32(idObj);

        if (await TableExistsAsync(cn, "contabilidad", "GuiaDetalle"))
            await InsertarDetalleDirectoAsync(cn, idGuia, req.Detalle ?? new List<GuiaDetalleItemRequest>());

        return idGuia;
    }

    private async Task InsertarDetalleDirectoAsync(SqlConnection cn, int idGuia, List<GuiaDetalleItemRequest> detalle)
    {
        var detCols = await GetColumnsAsync(cn, "contabilidad", "GuiaDetalle");
        if (detCols.Count == 0 || detalle.Count == 0) return;

        for (var i = 0; i < detalle.Count; i++)
        {
            var d = detalle[i];
            var cols = new List<string>();
            var vals = new List<string>();
            using var cmd = new SqlCommand();
            cmd.Connection = cn;

            void Add(string column, string parameter, object? value)
            {
                if (!detCols.Contains(column)) return;
                cols.Add(column);
                vals.Add(parameter);
                cmd.Parameters.AddWithValue(parameter, value ?? DBNull.Value);
            }

            Add("IdGuia", "@IdGuia", idGuia);
            Add("Item", "@Item", d.Item ?? (i + 1));
            Add("TipoItem", "@TipoItem", string.IsNullOrWhiteSpace(d.Tipo) ? "PRODUCTO" : d.Tipo);
            Add("Tipo", "@Tipo", string.IsNullOrWhiteSpace(d.Tipo) ? "PRODUCTO" : d.Tipo);
            Add("IdProducto", "@IdProducto", d.IdProducto);
            Add("IdVehiculo", "@IdVehiculo", d.IdVehiculo);
            Add("Descripcion", "@Descripcion", string.IsNullOrWhiteSpace(d.Descripcion) ? string.Empty : d.Descripcion);
            Add("Cantidad", "@Cantidad", d.Cantidad);
            Add("PrecioUnitario", "@PrecioUnitario", 0m);
            Add("Importe", "@Importe", 0m);
            Add("FechaCreacion", "@FechaCreacion", DateTime.Now);

            if (cols.Count == 0) continue;
            cmd.CommandText = $"INSERT INTO contabilidad.GuiaDetalle ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task<GuiaResponse?> ObtenerPorIdConSpAsync(SqlConnection cn, int idGuia)
    {
        using var cmd = new SqlCommand("contabilidad.usp_Guia_Get", cn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@IdGuia", idGuia);

        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        var guia = new GuiaResponse
        {
            IdGuia = SafeGetInt(rd, "IdGuia"),
            Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? string.Empty,
            Fecha = SafeGetDateTime(rd, "Fecha") ?? SafeGetDateTime(rd, "FechaEmision") ?? DateTime.UtcNow,
            Tipo = SafeGetString(rd, "Tipo"),
            Estado = SafeGetString(rd, "Estado") ?? "EMITIDA",
            MotivoTraslado = SafeGetString(rd, "MotivoTraslado") ?? SafeGetString(rd, "Motivo"),
            PuntoPartida = SafeGetString(rd, "PuntoPartida"),
            PuntoLlegada = SafeGetString(rd, "PuntoLlegada"),
            Destino = SafeGetString(rd, "Destino") ?? SafeGetString(rd, "Cliente"),
            Detalle = new List<GuiaDetalleItemResponse>()
        };

        if (await rd.NextResultAsync())
        {
            while (await rd.ReadAsync())
            {
                guia.Detalle!.Add(new GuiaDetalleItemResponse
                {
                    Item = SafeGetNullableInt(rd, "Item"),
                    Tipo = SafeGetString(rd, "Tipo"),
                    IdProducto = SafeGetNullableInt(rd, "IdProducto"),
                    IdVehiculo = SafeGetNullableInt(rd, "IdVehiculo"),
                    Descripcion = SafeGetString(rd, "Descripcion") ?? string.Empty,
                    Cantidad = SafeGetDecimal(rd, "Cantidad") ?? 0m
                });
            }
        }

        guia.TotalItems = guia.Detalle?.Count ?? 0;
        return guia;
    }

    private async Task<GuiaResponse?> ObtenerPorIdDirectoAsync(SqlConnection cn, int idGuia)
    {
        var guiaCols = await GetColumnsAsync(cn, "contabilidad", "Guia");
        if (guiaCols.Count == 0) return null;

        var numeroExpr = BuildNumeroExpression("g", guiaCols) + " AS Numero";
        var fechaExpr = BuildDateExpression("g", guiaCols) + " AS Fecha";
        var tipoExpr = BuildStringExpression("g", guiaCols, new[] { "Tipo" }, "Tipo");
        var estadoExpr = BuildStringExpression("g", guiaCols, new[] { "Estado" }, "Estado");
        var motivoExpr = BuildStringExpression("g", guiaCols, new[] { "MotivoTraslado", "Motivo" }, "MotivoTraslado");
        var partidaExpr = BuildStringExpression("g", guiaCols, new[] { "PuntoPartida" }, "PuntoPartida");
        var llegadaExpr = BuildStringExpression("g", guiaCols, new[] { "PuntoLlegada" }, "PuntoLlegada");

        var sql = $@"
SELECT
    g.IdGuia,
    {numeroExpr},
    {fechaExpr},
    {tipoExpr},
    {estadoExpr},
    {motivoExpr},
    {partidaExpr},
    {llegadaExpr}
FROM contabilidad.Guia g
WHERE g.IdGuia = @IdGuia;";

        GuiaResponse? guia = null;
        using (var cmd = new SqlCommand(sql, cn))
        {
            cmd.Parameters.AddWithValue("@IdGuia", idGuia);
            using var rd = await cmd.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                guia = new GuiaResponse
                {
                    IdGuia = SafeGetInt(rd, "IdGuia"),
                    Numero = SafeGetString(rd, "Numero") ?? string.Empty,
                    Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                    Tipo = SafeGetString(rd, "Tipo"),
                    Estado = SafeGetString(rd, "Estado") ?? string.Empty,
                    MotivoTraslado = SafeGetString(rd, "MotivoTraslado"),
                    PuntoPartida = SafeGetString(rd, "PuntoPartida"),
                    PuntoLlegada = SafeGetString(rd, "PuntoLlegada"),
                    Detalle = new List<GuiaDetalleItemResponse>()
                };
            }
        }

        if (guia == null) return null;

        if (await TableExistsAsync(cn, "contabilidad", "GuiaDetalle"))
        {
            var detCols = await GetColumnsAsync(cn, "contabilidad", "GuiaDetalle");
            var tipoDetExpr = detCols.Contains("TipoItem") ? "d.TipoItem AS Tipo" : detCols.Contains("Tipo") ? "d.Tipo AS Tipo" : "CAST(NULL AS NVARCHAR(50)) AS Tipo";

            var sqlDet = $@"
SELECT
    {(detCols.Contains("Item") ? "d.Item" : "CAST(NULL AS INT)")} AS Item,
    {tipoDetExpr},
    {(detCols.Contains("IdProducto") ? "d.IdProducto" : "CAST(NULL AS INT)")} AS IdProducto,
    {(detCols.Contains("IdVehiculo") ? "d.IdVehiculo" : "CAST(NULL AS INT)")} AS IdVehiculo,
    {(detCols.Contains("Descripcion") ? "d.Descripcion" : "CAST('' AS NVARCHAR(500))")} AS Descripcion,
    {(detCols.Contains("Cantidad") ? "d.Cantidad" : "CAST(0 AS DECIMAL(18,2))")} AS Cantidad
FROM contabilidad.GuiaDetalle d
WHERE d.IdGuia = @IdGuia
ORDER BY {(detCols.Contains("Item") ? "d.Item" : "(SELECT 1)")};";

            using var cmdDet = new SqlCommand(sqlDet, cn);
            cmdDet.Parameters.AddWithValue("@IdGuia", idGuia);
            using var rdDet = await cmdDet.ExecuteReaderAsync();
            while (await rdDet.ReadAsync())
            {
                guia.Detalle!.Add(new GuiaDetalleItemResponse
                {
                    Item = SafeGetNullableInt(rdDet, "Item"),
                    Tipo = SafeGetString(rdDet, "Tipo"),
                    IdProducto = SafeGetNullableInt(rdDet, "IdProducto"),
                    IdVehiculo = SafeGetNullableInt(rdDet, "IdVehiculo"),
                    Descripcion = SafeGetString(rdDet, "Descripcion") ?? string.Empty,
                    Cantidad = SafeGetDecimal(rdDet, "Cantidad") ?? 0m
                });
            }

            guia.TotalItems = guia.Detalle?.Count ?? 0;
        }

        return guia;
    }

    private async Task<List<GuiaResponse>> ListarConSpAsync(SqlConnection cn, string? q)
    {
        using var cmd = new SqlCommand("contabilidad.usp_Guia_List", cn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Q", string.IsNullOrWhiteSpace(q) ? DBNull.Value : q);

        var list = new List<GuiaResponse>();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new GuiaResponse
            {
                IdGuia = SafeGetInt(rd, "IdGuia"),
                Numero = SafeGetString(rd, "Numero") ?? SafeGetString(rd, "Referencia") ?? string.Empty,
                Fecha = SafeGetDateTime(rd, "Fecha") ?? SafeGetDateTime(rd, "FechaEmision") ?? DateTime.UtcNow,
                Tipo = SafeGetString(rd, "Tipo"),
                Estado = SafeGetString(rd, "Estado") ?? string.Empty,
                MotivoTraslado = SafeGetString(rd, "MotivoTraslado") ?? SafeGetString(rd, "Motivo"),
                PuntoPartida = SafeGetString(rd, "PuntoPartida"),
                PuntoLlegada = SafeGetString(rd, "PuntoLlegada")
            });
        }

        return list;
    }

    private async Task<List<GuiaResponse>> ListarDirectoAsync(SqlConnection cn, string? q)
    {
        var guiaCols = await GetColumnsAsync(cn, "contabilidad", "Guia");
        if (guiaCols.Count == 0) return new List<GuiaResponse>();

        var numeroExpr = BuildNumeroExpression("g", guiaCols) + " AS Numero";
        var fechaExpr = BuildDateExpression("g", guiaCols) + " AS Fecha";
        var tipoExpr = BuildStringExpression("g", guiaCols, new[] { "Tipo" }, "Tipo");
        var estadoExpr = BuildStringExpression("g", guiaCols, new[] { "Estado" }, "Estado");
        var motivoExpr = BuildStringExpression("g", guiaCols, new[] { "MotivoTraslado", "Motivo" }, "MotivoTraslado");
        var partidaExpr = BuildStringExpression("g", guiaCols, new[] { "PuntoPartida" }, "PuntoPartida");
        var llegadaExpr = BuildStringExpression("g", guiaCols, new[] { "PuntoLlegada" }, "PuntoLlegada");
        var totalItemsExpr = await TableExistsAsync(cn, "contabilidad", "GuiaDetalle")
            ? "(SELECT COUNT(1) FROM contabilidad.GuiaDetalle d WHERE d.IdGuia = g.IdGuia) AS TotalItems"
            : "CAST(NULL AS INT) AS TotalItems";

        var sql = $@"
SELECT TOP (200)
    g.IdGuia,
    {numeroExpr},
    {fechaExpr},
    {tipoExpr},
    {estadoExpr},
    {motivoExpr},
    {partidaExpr},
    {llegadaExpr},
    {totalItemsExpr}
FROM contabilidad.Guia g
WHERE (@Q IS NULL OR {BuildLikeExpression(guiaCols)})
ORDER BY g.IdGuia DESC;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Q", string.IsNullOrWhiteSpace(q) ? DBNull.Value : q);

        var list = new List<GuiaResponse>();
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new GuiaResponse
            {
                IdGuia = SafeGetInt(rd, "IdGuia"),
                Numero = SafeGetString(rd, "Numero") ?? string.Empty,
                Fecha = SafeGetDateTime(rd, "Fecha") ?? DateTime.UtcNow,
                Tipo = SafeGetString(rd, "Tipo"),
                Estado = SafeGetString(rd, "Estado") ?? string.Empty,
                MotivoTraslado = SafeGetString(rd, "MotivoTraslado"),
                PuntoPartida = SafeGetString(rd, "PuntoPartida"),
                PuntoLlegada = SafeGetString(rd, "PuntoLlegada"),
                TotalItems = SafeGetNullableInt(rd, "TotalItems")
            });
        }

        return list;
    }

    private static DataTable BuildDetalleTvp(List<GuiaDetalleItemRequest> detalle)
    {
        var dt = new DataTable();
        dt.Columns.Add("Item", typeof(int));
        dt.Columns.Add("TipoItem", typeof(string));
        dt.Columns.Add("IdProducto", typeof(int));
        dt.Columns.Add("IdVehiculo", typeof(int));
        dt.Columns.Add("Descripcion", typeof(string));
        dt.Columns.Add("Cantidad", typeof(decimal));
        dt.Columns.Add("PrecioUnitario", typeof(decimal));

        for (var i = 0; i < detalle.Count; i++)
        {
            var it = detalle[i];
            var row = dt.NewRow();
            row["Item"] = it.Item ?? (i + 1);
            row["TipoItem"] = string.IsNullOrWhiteSpace(it.Tipo) ? "PRODUCTO" : it.Tipo!;
            row["IdProducto"] = (object?)it.IdProducto ?? DBNull.Value;
            row["IdVehiculo"] = (object?)it.IdVehiculo ?? DBNull.Value;
            row["Descripcion"] = string.IsNullOrWhiteSpace(it.Descripcion) ? string.Empty : it.Descripcion;
            row["Cantidad"] = it.Cantidad;
            row["PrecioUnitario"] = 0m;
            dt.Rows.Add(row);
        }

        return dt;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildNumeroExpression(string alias, HashSet<string> cols)
    {
        if (cols.Contains("Serie") && cols.Contains("Numero"))
            return $"CONCAT(ISNULL({alias}.Serie, ''), '-', RIGHT('00000000' + CAST({alias}.Numero AS VARCHAR(20)), 8))";

        if (cols.Contains("Numero"))
            return $"CAST({alias}.Numero AS VARCHAR(50))";

        if (cols.Contains("Referencia"))
            return $"CAST({alias}.Referencia AS VARCHAR(50))";

        return "CAST('' AS NVARCHAR(50))";
    }

    private static string BuildDateExpression(string alias, HashSet<string> cols)
    {
        if (cols.Contains("FechaEmision")) return $"CAST({alias}.FechaEmision AS DATETIME)";
        if (cols.Contains("Fecha")) return $"CAST({alias}.Fecha AS DATETIME)";
        if (cols.Contains("FechaCreacion")) return $"CAST({alias}.FechaCreacion AS DATETIME)";
        return "GETDATE()";
    }

    private static string BuildStringExpression(string alias, HashSet<string> cols, IEnumerable<string> candidates, string outputAlias)
    {
        foreach (var c in candidates)
        {
            if (cols.Contains(c)) return $"CAST({alias}.{c} AS NVARCHAR(200)) AS {outputAlias}";
        }

        return $"CAST(NULL AS NVARCHAR(200)) AS {outputAlias}";
    }

    private static string BuildLikeExpression(HashSet<string> cols)
    {
        var terms = new List<string>();

        if (cols.Contains("Serie") && cols.Contains("Numero"))
            terms.Add("CONCAT(ISNULL(g.Serie, ''), '-', RIGHT('00000000' + CAST(g.Numero AS VARCHAR(20)), 8)) LIKE '%' + @Q + '%'");
        else if (cols.Contains("Numero"))
            terms.Add("CAST(g.Numero AS VARCHAR(50)) LIKE '%' + @Q + '%'");

        if (cols.Contains("Tipo")) terms.Add("ISNULL(g.Tipo, '') LIKE '%' + @Q + '%'");
        if (cols.Contains("MotivoTraslado")) terms.Add("ISNULL(g.MotivoTraslado, '') LIKE '%' + @Q + '%'");
        else if (cols.Contains("Motivo")) terms.Add("ISNULL(g.Motivo, '') LIKE '%' + @Q + '%'");
        if (cols.Contains("PuntoPartida")) terms.Add("ISNULL(g.PuntoPartida, '') LIKE '%' + @Q + '%'");
        if (cols.Contains("PuntoLlegada")) terms.Add("ISNULL(g.PuntoLlegada, '') LIKE '%' + @Q + '%'");

        return terms.Count == 0 ? "1 = 1" : string.Join(" OR ", terms);
    }

    private static async Task<bool> ProcedureExistsAsync(SqlConnection cn, string schema, string procedureName)
    {
        const string sql = @"
SELECT 1
FROM sys.procedures p
INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
WHERE s.name = @Schema AND p.name = @Name;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Schema", schema);
        cmd.Parameters.AddWithValue("@Name", procedureName);

        return (await cmd.ExecuteScalarAsync()) != null;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection cn, string schema, string tableName)
    {
        const string sql = @"
SELECT 1
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @Schema AND t.name = @Name;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Schema", schema);
        cmd.Parameters.AddWithValue("@Name", tableName);

        return (await cmd.ExecuteScalarAsync()) != null;
    }

    private static async Task<HashSet<string>> GetColumnsAsync(SqlConnection cn, string schema, string tableName)
    {
        const string sql = @"
SELECT c.name
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @Schema AND t.name = @Table;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Schema", schema);
        cmd.Parameters.AddWithValue("@Table", tableName);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            if (!rd.IsDBNull(0)) result.Add(rd.GetString(0));
        }

        return result;
    }

    private static async Task<bool> ProcedureHasParameterAsync(SqlConnection cn, string schema, string procedureName, string parameterName)
    {
        const string sql = @"
SELECT 1
FROM sys.parameters p
INNER JOIN sys.procedures pr ON pr.object_id = p.object_id
INNER JOIN sys.schemas s ON s.schema_id = pr.schema_id
WHERE s.name = @Schema AND pr.name = @Procedure AND p.name = @Parameter;";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Schema", schema);
        cmd.Parameters.AddWithValue("@Procedure", procedureName);
        cmd.Parameters.AddWithValue("@Parameter", parameterName);

        return (await cmd.ExecuteScalarAsync()) != null;
    }

    private static async Task AddProcParameterIfExistsAsync(SqlConnection cn, SqlCommand cmd, string schema, string procedureName, string parameterName, object? value)
    {
        if (!await ProcedureHasParameterAsync(cn, schema, procedureName, parameterName)) return;
        cmd.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
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
        for (var i = 0; i < rd.FieldCount; i++)
        {
            if (string.Equals(rd.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
