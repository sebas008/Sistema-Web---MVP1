using CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class FacturacionService : IFacturacionService
{
    private readonly IFacturacionRepository _repo;
    public FacturacionService(IFacturacionRepository repo) => _repo = repo;

    public async Task<FacturaResponse> EmitirAsync(FacturaEmitirRequest req)
    {
        var id = await _repo.EmitirAsync(req);
        var factura = await _repo.ObtenerPorIdAsync(id);

        return factura ?? new FacturaResponse
        {
            IdFactura = id,
            Numero = $"FACT-{id:00000000}",
            Estado = "EMITIDA",
            Fecha = req.FechaEmision,
            Cliente = null,
            Total = 0m
        };
    }

    public Task<FacturaResponse?> ObtenerPorIdAsync(int idFactura)
        => _repo.ObtenerPorIdAsync(idFactura);

    public Task<List<FacturaResponse>> ListarAsync(string? q)
        => _repo.ListarAsync(q);

    public Task AnularAsync(int idFactura)
        => _repo.AnularAsync(idFactura);
}
