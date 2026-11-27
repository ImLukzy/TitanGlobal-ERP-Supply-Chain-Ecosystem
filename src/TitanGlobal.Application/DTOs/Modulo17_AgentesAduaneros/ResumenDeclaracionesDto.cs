namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class ResumenDeclaracionesDto
{
    public int TotalDeclaraciones { get; set; }

    public decimal DerechosArancelVigentes { get; set; }

    public List<EstadoDeclaracionConteoDto> PorEstado { get; set; } = new List<EstadoDeclaracionConteoDto>();
}
