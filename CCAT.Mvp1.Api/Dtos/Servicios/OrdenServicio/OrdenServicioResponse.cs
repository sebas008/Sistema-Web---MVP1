namespace CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;

public class OrdenServicioResponse
{
    public int IdOrdenServicio { get; set; }
    public string? NumeroOS { get; set; }
    public int? IdCliente { get; set; }
    public string? Placa { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int? Kilometraje { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public DateTime? FechaSalida { get; set; }
    public string Estado { get; set; } = "";
    public string? Observacion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal IGV { get; set; }
    public decimal Total { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? UsuarioCreacion { get; set; }

    public List<OrdenServicioDetalleResponse> Detalle { get; set; } = new();
}