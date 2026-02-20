using CCAT.Mvp1.Api.DTOs.Clientes;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IClienteRepository
{
    Task<List<ClienteResponse>> ListarAsync(string? q, bool? soloActivos);
    Task<ClienteResponse?> ObtenerAsync(int idCliente);

    Task<int> CrearAsync(ClienteCrearRequest req);
    Task<int> ActualizarAsync(int idCliente, ClienteActualizarRequest req);

    Task CambiarEstadoAsync(int idCliente, bool activo);
}