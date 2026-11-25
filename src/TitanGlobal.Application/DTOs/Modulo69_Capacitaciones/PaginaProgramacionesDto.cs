namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class PaginaProgramacionesDto
{
    public List<ProgramacionListadoDto> Items { get; set; } = new List<ProgramacionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
