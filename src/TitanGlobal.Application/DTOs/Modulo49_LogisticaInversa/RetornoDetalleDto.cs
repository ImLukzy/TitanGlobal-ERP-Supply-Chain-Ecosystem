namespace TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;

public class RetornoDetalleDto : RetornoListadoDto
{
    public string? Observaciones { get; set; }
    public string? CantidadRecogida { get; set; }
    public string? CantidadRecibida { get; set; }
    public string? NoRecogida { get; set; }
    public string? NoRecibida { get; set; }
    public string? ResponsableRecogida { get; set; }
    public string? ResponsableRecepcion { get; set; }
    public string? MotivoCancelacion { get; set; }
    public DateTime? FechaRecogida { get; set; }
    public DateTime? FechaRecepcion { get; set; }
    public DateTime? FechaCancelacion { get; set; }

}
