namespace CCAT.Mvp1.Api.DTOs.Contabilidad.Guias;

public class GuiaResponse
{
    public int IdGuia { get; set; }
    public string Numero { get; set; } = "";   // ej: GUIA-0001
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";   // EMITIDA / ANULADA
    public string? Destino { get; set; }       // Cliente/Almacén/Sucursal
}