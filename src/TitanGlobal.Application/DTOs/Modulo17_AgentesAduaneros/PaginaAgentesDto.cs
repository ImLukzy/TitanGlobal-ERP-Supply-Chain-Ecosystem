namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class PaginaAgentesDto
{
    public List<AgenteDto> Items { get; set; } = new List<AgenteDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
