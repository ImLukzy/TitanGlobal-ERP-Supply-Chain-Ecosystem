namespace TitanGlobal.Api.Modules.Modulo42_Conductores.Dtos;

public class PaginaConductoresDto
{
    public List<ConductorListadoDto> Items { get; set; } = new List<ConductorListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
