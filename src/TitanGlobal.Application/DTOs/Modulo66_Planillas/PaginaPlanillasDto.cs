namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class PaginaPlanillasDto
{
    public List<PlanillaListadoDto> Items { get; set; } = new List<PlanillaListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
