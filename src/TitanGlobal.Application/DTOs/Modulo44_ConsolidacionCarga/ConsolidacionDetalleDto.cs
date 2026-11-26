namespace TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;

public class ConsolidacionDetalleDto : ConsolidacionListadoDto
{
    public string? Observaciones { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? MotivoCancelacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public List<LineaCargaDto> Lineas { get; set; } = new List<LineaCargaDto>();
}
