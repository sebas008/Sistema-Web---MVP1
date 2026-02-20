namespace CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;

public class OrdenServicioCrearRequest
{
    public int? IdCliente { get; set; }
    public string? Placa { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int? Kilometraje { get; set; }
    public string? Observacion { get; set; }

    public string Usuario { get; set; } = "admin";
}