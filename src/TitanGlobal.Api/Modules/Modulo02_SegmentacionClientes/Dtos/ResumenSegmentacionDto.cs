namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;

public class ResumenSegmentacionDto
{
    public int TotalClientes { get; set; }
    public int ClientesSegmentados { get; set; }
    public int ClientesSinSegmento { get; set; }
    public List<SegmentoClientesDto> PorSegmento { get; set; } = new List<SegmentoClientesDto>();
}
