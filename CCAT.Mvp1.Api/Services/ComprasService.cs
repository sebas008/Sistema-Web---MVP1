using CCAT.Mvp1.Api.DTOs.Contabilidad.Compras;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class ComprasService : IComprasService
{
    private readonly IComprasRepository _repo;
    public ComprasService(IComprasRepository repo) => _repo = repo;

    public async Task<CompraResponse> RegistrarAsync(CompraRegistrarRequest req)
    {
        var id = await _repo.RegistrarAsync(req);
        var compra = await _repo.ObtenerPorIdAsync(id);

        if (compra is null)
            return new CompraResponse { IdCompra = id, Numero = $"COMP-{id:0000}", Estado = "REGISTRADA", Fecha = DateTime.UtcNow, Total = 0m };

        return compra;
    }

    public Task<CompraResponse?> ObtenerPorIdAsync(int idCompra) => _repo.ObtenerPorIdAsync(idCompra);

    public Task<List<CompraResponse>> ListarAsync(string? q) => _repo.ListarAsync(q);
}