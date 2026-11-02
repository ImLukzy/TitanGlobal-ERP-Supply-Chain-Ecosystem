namespace TitanGlobal.Application.DTOs.Modulo42_Conductores;

public class PaginaConductoresDto
{
    public List<ConductorListadoDto> Items { get; set; } = new List<ConductorListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
