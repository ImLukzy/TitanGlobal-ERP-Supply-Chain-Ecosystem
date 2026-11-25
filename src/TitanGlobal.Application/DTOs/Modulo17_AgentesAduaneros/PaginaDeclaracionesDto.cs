namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class PaginaDeclaracionesDto
{
    public List<DeclaracionListadoDto> Items { get; set; } = new List<DeclaracionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
