using System.Text.Json.Serialization;

namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;

public class GuiaEmitirRequest
{
    public string? Serie { get; set; } = "G001";
    public string? FechaEmision { get; set; }
    public string? Tipo { get; set; } = "REMISION";
    public string? MotivoTraslado { get; set; }
    public string? PuntoPartida { get; set; }
    public string? PuntoLlegada { get; set; }
    public bool AfectaStock { get; set; } = true;
    public List<GuiaDetalleItemRequest> Detalle { get; set; } = new();
    public string Usuario { get; set; } = "admin";

    // Compatibilidad con el DTO antiguo / SP legado
    public int IdCliente { get; set; }
    public DateTime? Fecha { get; set; }
    public string? DetalleJson { get; set; }

    [JsonIgnore]
    public DateTime FechaOperacion
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FechaEmision) && DateTime.TryParse(FechaEmision, out var fecha))
                return fecha.Date;

            return Fecha?.Date ?? DateTime.Today;
        }
    }
}

public class GuiaDetalleItemRequest
{
    public int? Item { get; set; }
    public string? Tipo { get; set; }
    public int? IdProducto { get; set; }
    public int? IdVehiculo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
}
