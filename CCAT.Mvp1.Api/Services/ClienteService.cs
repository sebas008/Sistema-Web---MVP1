using CCAT.Mvp1.Api.DTOs.Clientes;
using CCAT.Mvp1.Api.Interfaces;

namespace CCAT.Mvp1.Api.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repo;
    public ClienteService(IClienteRepository repo) => _repo = repo;

    public Task<List<ClienteResponse>> ListarAsync(string? q, bool? soloActivos)
        => _repo.ListarAsync(q, soloActivos);

    public Task<ClienteResponse?> ObtenerAsync(int idCliente)
        => _repo.ObtenerAsync(idCliente);

    public async Task<ClienteResponse> CrearAsync(ClienteCrearRequest req)
    {
        var id = await _repo.CrearAsync(req);
        var creado = await _repo.ObtenerAsync(id);
        return creado ?? throw new Exception("No se pudo obtener el cliente creado.");
    }

    public async Task<ClienteResponse> ActualizarAsync(int idCliente, ClienteActualizarRequest req)
    {
        await _repo.ActualizarAsync(idCliente, req);
        var upd = await _repo.ObtenerAsync(idCliente);
        return upd ?? throw new Exception("No se pudo obtener el cliente actualizado.");
    }

    public async Task<ClienteResponse> CambiarEstadoAsync(int idCliente, bool activo)
    {
        await _repo.CambiarEstadoAsync(idCliente, activo);
        var upd = await _repo.ObtenerAsync(idCliente);
        return upd ?? throw new Exception("No se pudo obtener el cliente luego del cambio de estado.");
    }
}