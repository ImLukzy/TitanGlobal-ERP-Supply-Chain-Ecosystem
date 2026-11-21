namespace TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;

public class RegistrarPodRequestDto
{
    public string Receptor { get; set; } = string.Empty;

    public string DocumentoReceptor { get; set; } = string.Empty;

    public string ReferenciaConstancia { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}
