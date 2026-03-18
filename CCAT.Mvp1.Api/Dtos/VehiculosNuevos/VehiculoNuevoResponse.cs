namespace CCAT.Mvp1.Api.DTOs.VehiculosNuevos;

public class VehiculoNuevoResponse
{
    public int IdVehiculo { get; set; }

    /* base */
    public string? VIN { get; set; }
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public int Anio { get; set; }
    public string? Version { get; set; }
    public string? Color { get; set; }
    public decimal PrecioLista { get; set; }
    public bool Activo { get; set; }
    public decimal StockActual { get; set; }

    /* nuevos */
    public string? CodigoVehiculo { get; set; }
    public string? CodigoExterno { get; set; }
    public string? ModeloLegal { get; set; }
    public string? TipoVehiculo { get; set; }
    public decimal? PrecioCompra { get; set; }
    public decimal? PrecioVenta { get; set; }
    public string? TipoTransmision { get; set; }
    public string? ColorExterior { get; set; }
    public string? ColorInterior { get; set; }
    public int? NumeroAsientos { get; set; }
    public int? NumeroPuertas { get; set; }
    public string? CilindrajeCc { get; set; }
    public string? PotenciaHp { get; set; }
    public string? TipoCombustible { get; set; }
    public string? NumeroMotor { get; set; }
    public string? NumeroChasis { get; set; }
    public string? ModeloTecnico { get; set; }
    public string? CodigoSap { get; set; }
    public decimal? PesoBruto { get; set; }
    public decimal? CargaUtil { get; set; }
    public string? EstadoVehiculo { get; set; }
    public string? Ubicacion { get; set; }
    public string? SeccionAsignada { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public bool? Catalitico { get; set; }
    public string? TipoCatalitico { get; set; }
    public decimal? BonoUsd { get; set; }
    public bool? Pagado { get; set; }
    public bool? TestDrive { get; set; }
    public string? UnidadTestDrive { get; set; }
    public bool? Km0 { get; set; }
    public string? Observacion { get; set; }
}
