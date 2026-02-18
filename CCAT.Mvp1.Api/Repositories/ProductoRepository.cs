using System.Data;
using Dapper;
using CCAT.Mvp1.Api.DTOs.Inventario;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly IDbConnectionFactory _factory;
    public ProductoRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<ProductoResponse> CrearAsync(ProductoCrearRequest req)
    {
        using var cn = _factory.CreateConnection();
        return await cn.QuerySingleAsync<ProductoResponse>(
            "inventario.usp_Producto_Crear",
            req,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<List<ProductoResponse>> ListarAsync(string? q, bool? activo)
    {
        using var cn = _factory.CreateConnection();
        var rows = await cn.QueryAsync<ProductoResponse>(
            "inventario.usp_Producto_Listar",
            new { Q = q, Activo = activo },
            commandType: CommandType.StoredProcedure
        );
        return rows.ToList();
    }

    public async Task<ProductoResponse?> ObtenerPorIdAsync(int productoId)
    {
        using var cn = _factory.CreateConnection();
        return await cn.QueryFirstOrDefaultAsync<ProductoResponse>(
            "inventario.usp_Producto_ObtenerPorId",
            new { ProductoId = productoId },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<ProductoResponse> ActualizarAsync(int productoId, ProductoActualizarRequest req)
    {
        using var cn = _factory.CreateConnection();
        return await cn.QuerySingleAsync<ProductoResponse>(
            "inventario.usp_Producto_Actualizar",
            new { ProductoId = productoId, req.Nombre, req.Descripcion, req.Precio },
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<ProductoResponse> CambiarEstadoAsync(int productoId, bool activo)
    {
        using var cn = _factory.CreateConnection();
        return await cn.QuerySingleAsync<ProductoResponse>(
            "inventario.usp_Producto_CambiarEstado",
            new { ProductoId = productoId, Activo = activo },
            commandType: CommandType.StoredProcedure
        );
    }
}
