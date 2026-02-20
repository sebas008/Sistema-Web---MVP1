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

        // Intentamos devolver la factura completa (si tienes usp_Factura_Get)
        var factura = await _repo.ObtenerPorIdAsync(id);
        if (factura is null)
        {
            // fallback mínimo para no romper el flujo
            return new FacturaResponse { IdFactura = id, Numero = $"FACT-{id:0000}", Estado = "EMITIDA", Fecha = DateTime.UtcNow, Total = 0m };
        }

        return factura;
    }

    public Task<FacturaResponse?> ObtenerPorIdAsync(int idFactura)
        => _repo.ObtenerPorIdAsync(idFactura);

    public Task<List<FacturaResponse>> ListarAsync(string? q)
        => _repo.ListarAsync(q);
}