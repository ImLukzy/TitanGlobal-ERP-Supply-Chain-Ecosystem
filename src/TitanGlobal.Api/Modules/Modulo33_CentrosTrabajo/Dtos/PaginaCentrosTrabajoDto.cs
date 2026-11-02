namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.Dtos;

public class PaginaCentrosTrabajoDto
{
    public List<CentroTrabajoListadoDto> Items { get; set; } = new List<CentroTrabajoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
