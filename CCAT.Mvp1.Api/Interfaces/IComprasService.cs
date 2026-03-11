using CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IComprasService
{
    Task<CompraResponse> RegistrarAsync(CompraRegistrarRequest req);
    Task<CompraResponse?> ObtenerPorIdAsync(int idCompra);
    Task<List<CompraResponse>> ListarAsync(string? q);
    Task<bool> AnularAsync(int idCompra);
}
