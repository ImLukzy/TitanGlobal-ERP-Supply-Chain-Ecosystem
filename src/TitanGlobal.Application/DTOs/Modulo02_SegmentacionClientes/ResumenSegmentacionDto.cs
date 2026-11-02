namespace TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;

public class ResumenSegmentacionDto
{
    public int TotalClientes { get; set; }
    public int ClientesSegmentados { get; set; }
    public int ClientesSinSegmento { get; set; }
    public List<SegmentoClientesDto> PorSegmento { get; set; } = new List<SegmentoClientesDto>();
}
