namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class PaginaCiclosEvaluacionDto
{
    public List<CicloEvaluacionListadoDto> Items { get; set; } = new List<CicloEvaluacionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
