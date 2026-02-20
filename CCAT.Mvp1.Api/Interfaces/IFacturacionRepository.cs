using CCAT.Mvp1.Api.DTOs.Contabilidad.Facturacion;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IFacturacionRepository
{
    Task<int> EmitirAsync(FacturaEmitirRequest req);
    Task<FacturaResponse?> ObtenerPorIdAsync(int idFactura);
    Task<List<FacturaResponse>> ListarAsync(string? q);
}