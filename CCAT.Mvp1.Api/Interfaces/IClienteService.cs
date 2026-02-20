using CCAT.Mvp1.Api.DTOs.Clientes;

namespace CCAT.Mvp1.Api.Interfaces;

public interface IClienteService
{
    Task<List<ClienteResponse>> ListarAsync(string? q, bool? soloActivos);
    Task<ClienteResponse?> ObtenerAsync(int idCliente);

    Task<ClienteResponse> CrearAsync(ClienteCrearRequest req);
    Task<ClienteResponse> ActualizarAsync(int idCliente, ClienteActualizarRequest req);

    Task<ClienteResponse> CambiarEstadoAsync(int idCliente, bool activo);
}