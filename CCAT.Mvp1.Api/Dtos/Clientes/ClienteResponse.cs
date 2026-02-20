namespace CCAT.Mvp1.Api.DTOs.Clientes;

public class ClienteResponse
{
    public int IdCliente { get; set; }
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string RazonSocial { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? UsuarioCreacion { get; set; }
}