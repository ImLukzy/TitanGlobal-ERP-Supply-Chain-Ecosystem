namespace TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;

public class SiniestroDetalleDto : SiniestroListadoDto
{
    public string? Observaciones { get; set; }
    public string? NumeroPoliza { get; set; }
    public string? Aseguradora { get; set; }
    public string? NumeroReclamo { get; set; }
    public string? Moneda { get; set; }
    public string? MontoReclamado { get; set; }
    public string? Resultado { get; set; }
    public string? MontoReconocido { get; set; }
    public string? Resolucion { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaReclamo { get; set; }
    public DateTime? FechaCierre { get; set; }
    public DateTime? FechaAnulacion { get; set; }
}
