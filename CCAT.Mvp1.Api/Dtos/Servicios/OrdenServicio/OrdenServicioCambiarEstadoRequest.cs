namespace CCAT.Mvp1.Api.DTOs.Servicios.OrdenServicio;

public class OrdenServicioCambiarEstadoRequest
{
    public string Estado { get; set; } = "ABIERTA"; // ABIERTA / EN_PROCESO / CERRADA / ANULADA
}