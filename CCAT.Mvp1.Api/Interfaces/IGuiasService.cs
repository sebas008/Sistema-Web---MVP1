using CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IGuiasService
{
    Task<GuiaResponse> EmitirAsync(GuiaEmitirRequest req);
    Task<GuiaResponse?> ObtenerPorIdAsync(int idGuia);
    Task<List<GuiaResponse>> ListarAsync(string? q);
}