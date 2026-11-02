namespace TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;

public class PaginaCentrosTrabajoDto
{
    public List<CentroTrabajoListadoDto> Items { get; set; } = new List<CentroTrabajoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
