namespace TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;

public class PaginaConsolidacionesDto
{
    public List<ConsolidacionListadoDto> Items { get; set; } = new List<ConsolidacionListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
