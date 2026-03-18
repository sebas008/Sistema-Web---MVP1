namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;

public class GuiaResponse
{
    public int IdGuia { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Tipo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoTraslado { get; set; }
    public string? PuntoPartida { get; set; }
    public string? PuntoLlegada { get; set; }
    public int? TotalItems { get; set; }
    public string? Destino { get; set; }
    public List<GuiaDetalleItemResponse>? Detalle { get; set; }
}

public class GuiaDetalleItemResponse
{
    public int? Item { get; set; }
    public string? Tipo { get; set; }
    public int? IdProducto { get; set; }
    public int? IdVehiculo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
}
