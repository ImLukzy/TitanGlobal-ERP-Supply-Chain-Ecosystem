namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class PaginaAcuerdosDto
{
    public List<AcuerdoListadoDto> Items { get; set; } = new List<AcuerdoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
