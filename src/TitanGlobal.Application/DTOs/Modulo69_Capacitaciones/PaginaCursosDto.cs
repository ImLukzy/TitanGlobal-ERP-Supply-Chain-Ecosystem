namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class PaginaCursosDto
{
    public List<CursoListadoDto> Items { get; set; } = new List<CursoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
