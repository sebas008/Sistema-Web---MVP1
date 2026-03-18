using CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class GuiasService : IGuiasService
{
    private readonly IGuiasRepository _repo;

    public GuiasService(IGuiasRepository repo)
    {
        _repo = repo;
    }

    public async Task<GuiaResponse> EmitirAsync(GuiaEmitirRequest req)
    {
        var id = await _repo.EmitirAsync(req);
        var guia = await _repo.ObtenerPorIdAsync(id);

        return guia ?? new GuiaResponse
        {
            IdGuia = id,
            Numero = $"GUIA-{id:0000}",
            Fecha = req.FechaOperacion,
            Tipo = req.Tipo,
            Estado = "EMITIDA",
            MotivoTraslado = req.MotivoTraslado,
            PuntoPartida = req.PuntoPartida,
            PuntoLlegada = req.PuntoLlegada,
            TotalItems = req.Detalle?.Count ?? 0,
            Detalle = req.Detalle?.Select(x => new GuiaDetalleItemResponse
            {
                Item = x.Item,
                Tipo = x.Tipo,
                IdProducto = x.IdProducto,
                IdVehiculo = x.IdVehiculo,
                Descripcion = x.Descripcion,
                Cantidad = x.Cantidad
            }).ToList()
        };
    }

    public Task<GuiaResponse?> ObtenerPorIdAsync(int idGuia) => _repo.ObtenerPorIdAsync(idGuia);

    public Task<List<GuiaResponse>> ListarAsync(string? q) => _repo.ListarAsync(q);

    public Task<bool> AnularAsync(int idGuia) => _repo.AnularAsync(idGuia);
}
