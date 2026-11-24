namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class PaginaBonosPlanillaDto
{
    public List<BonoPlanillaDto> Items { get; set; } = new List<BonoPlanillaDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
