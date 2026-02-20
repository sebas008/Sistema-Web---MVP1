namespace CCAT.Mvp1.Api.DTOs.Clientes;

public class ClienteCrearRequest
{
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string RazonSocial { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    // auditoría
    public string Usuario { get; set; } = "admin";
}