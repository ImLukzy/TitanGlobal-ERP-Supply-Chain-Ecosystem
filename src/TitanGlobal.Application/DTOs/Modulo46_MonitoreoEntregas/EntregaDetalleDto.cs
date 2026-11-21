namespace TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;

public class EntregaDetalleDto : EntregaListadoDto
{
    public string? Observaciones { get; set; }

    public DateTime? FechaSalida { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? Receptor { get; set; }

    public string? DocumentoReceptor { get; set; }

    public string? ReferenciaConstancia { get; set; }

    public string? ObservacionesEntrega { get; set; }

    public string? MotivoCancelacion { get; set; }

    public List<EventoEntregaDto> Historial { get; set; } = new();
}
