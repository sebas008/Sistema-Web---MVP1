using System.Data;
using Microsoft.Data.SqlClient;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

// Implementación alineada al script SISTEMA_CCAT.sql (sin SPs de Producto)
public class ProductoRepository : IProductoRepository
{
    private readonly IDbConnectionFactory _factory;
    public ProductoRepository(IDbConnectionFactory factory) => _factory = factory;

    private static object DbOrNull(string? v) => string.IsNullOrWhiteSpace(v) ? DBNull.Value : v;

    public async Task<ProductoResponse> CrearAsync(ProductoCrearRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
INSERT INTO inventario.Producto (Nombre, TipoProducto, Codigo, Precio, Activo, FechaCreacion, UsuarioCreacion)
VALUES (@nombre, @tipo, @codigo, @precio, 1, SYSDATETIME(), @usuario);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@nombre", req.Nombre);
        cmd.Parameters.AddWithValue("@tipo", DbOrNull(req.Descripcion));
        cmd.Parameters.AddWithValue("@codigo", DbOrNull(req.Codigo));
        cmd.Parameters.AddWithValue("@precio", (object?)req.Precio ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@usuario", "admin");

        var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        var created = await ObtenerPorIdAsync(newId);
        return created ?? throw new Exception("No se pudo obtener el producto creado.");
    }

    public async Task<List<ProductoResponse>> ListarAsync(string? q, bool? activo)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT TOP (500)
    p.IdProducto,
    p.Nombre,
    p.TipoProducto,
    ISNULL(p.Codigo,'') AS Codigo,
    ISNULL(p.Precio,0) AS Precio,
    p.Activo,
    p.FechaCreacion
FROM inventario.Producto p
WHERE (@activo IS NULL OR p.Activo = @activo)
  AND (
        @q IS NULL
        OR p.Nombre LIKE '%' + @q + '%'
        OR p.Codigo LIKE '%' + @q + '%'
      )
ORDER BY p.IdProducto DESC;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@q", DbOrNull(q));
        cmd.Parameters.AddWithValue("@activo", (object?)activo ?? DBNull.Value);

        var list = new List<ProductoResponse>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new ProductoResponse
            {
                ProductoId = rd.GetInt32(rd.GetOrdinal("IdProducto")),
                Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
                Descripcion = rd.IsDBNull(rd.GetOrdinal("TipoProducto")) ? null : rd.GetString(rd.GetOrdinal("TipoProducto")),
                Codigo = rd.GetString(rd.GetOrdinal("Codigo")),
                Precio = rd.GetDecimal(rd.GetOrdinal("Precio")),
                Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
                CreadoEn = rd.GetDateTime(rd.GetOrdinal("FechaCreacion"))
            });
        }
        return list;
    }

    public async Task<ProductoResponse?> ObtenerPorIdAsync(int productoId)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
SELECT
    p.IdProducto,
    p.Nombre,
    p.TipoProducto,
    ISNULL(p.Codigo,'') AS Codigo,
    ISNULL(p.Precio,0) AS Precio,
    p.Activo,
    p.FechaCreacion
FROM inventario.Producto p
WHERE p.IdProducto = @id;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", productoId);

        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) return null;

        return new ProductoResponse
        {
            ProductoId = rd.GetInt32(rd.GetOrdinal("IdProducto")),
            Nombre = rd.GetString(rd.GetOrdinal("Nombre")),
            Descripcion = rd.IsDBNull(rd.GetOrdinal("TipoProducto")) ? null : rd.GetString(rd.GetOrdinal("TipoProducto")),
            Codigo = rd.GetString(rd.GetOrdinal("Codigo")),
            Precio = rd.GetDecimal(rd.GetOrdinal("Precio")),
            Activo = rd.GetBoolean(rd.GetOrdinal("Activo")),
            CreadoEn = rd.GetDateTime(rd.GetOrdinal("FechaCreacion"))
        };
    }

    public async Task<ProductoResponse> ActualizarAsync(int productoId, ProductoActualizarRequest req)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
UPDATE inventario.Producto
SET Nombre = @nombre,
    TipoProducto = @tipo,
    Precio = @precio,
    FechaActualizacion = SYSDATETIME(),
    UsuarioActualizacion = @usuario
WHERE IdProducto = @id;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", productoId);
        cmd.Parameters.AddWithValue("@nombre", req.Nombre);
        cmd.Parameters.AddWithValue("@tipo", DbOrNull(req.Descripcion));
        cmd.Parameters.AddWithValue("@precio", (object?)req.Precio ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@usuario", "admin");
        await cmd.ExecuteNonQueryAsync();

        var updated = await ObtenerPorIdAsync(productoId);
        return updated ?? throw new Exception("No se pudo obtener el producto actualizado.");
    }

    public async Task<ProductoResponse> CambiarEstadoAsync(int productoId, bool activo)
    {
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        var sql = @"
UPDATE inventario.Producto
SET Activo = @activo,
    FechaActualizacion = SYSDATETIME(),
    UsuarioActualizacion = @usuario
WHERE IdProducto = @id;";

        await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
        cmd.Parameters.AddWithValue("@id", productoId);
        cmd.Parameters.AddWithValue("@activo", activo);
        cmd.Parameters.AddWithValue("@usuario", "admin");
        await cmd.ExecuteNonQueryAsync();

        var updated = await ObtenerPorIdAsync(productoId);
        return updated ?? throw new Exception("No se pudo obtener el producto actualizado.");
    }
}
