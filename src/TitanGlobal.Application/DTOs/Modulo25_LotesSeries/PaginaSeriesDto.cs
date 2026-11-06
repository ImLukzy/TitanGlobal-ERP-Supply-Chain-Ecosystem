namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class PaginaSeriesDto
{
    public List<SerieListadoDto> Items { get; set; } = new List<SerieListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
