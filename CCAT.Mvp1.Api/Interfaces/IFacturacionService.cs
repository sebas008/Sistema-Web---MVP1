using CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IFacturacionService
{
    Task<FacturaResponse> EmitirAsync(FacturaEmitirRequest req);
    Task<FacturaResponse?> ObtenerPorIdAsync(int idFactura);
    Task<List<FacturaResponse>> ListarAsync(string? q);
    Task AnularAsync(int idFactura);
}
