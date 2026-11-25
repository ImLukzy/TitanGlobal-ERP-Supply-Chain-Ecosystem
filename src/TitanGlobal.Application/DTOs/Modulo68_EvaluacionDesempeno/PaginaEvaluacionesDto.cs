namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class PaginaEvaluacionesDto
{
    public List<EvaluacionListadoDto> Items { get; set; } = new List<EvaluacionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
