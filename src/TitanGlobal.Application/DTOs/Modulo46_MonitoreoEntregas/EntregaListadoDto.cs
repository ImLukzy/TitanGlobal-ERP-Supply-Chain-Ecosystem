namespace TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;

public class EntregaListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string ReferenciaGuia { get; set; } = string.Empty;

    public string Destinatario { get; set; } = string.Empty;

    public string DireccionDestino { get; set; } = string.Empty;

    public DateTime FechaProgramada { get; set; }

    public string Estado { get; set; } = string.Empty;
}
