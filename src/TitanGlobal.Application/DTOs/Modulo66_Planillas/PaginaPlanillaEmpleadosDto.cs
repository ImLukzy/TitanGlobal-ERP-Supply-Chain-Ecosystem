namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class PaginaPlanillaEmpleadosDto
{
    public List<PlanillaEmpleadoDto> Items { get; set; } = new List<PlanillaEmpleadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
