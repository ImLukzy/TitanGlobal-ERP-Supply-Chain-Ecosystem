namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class PaginaLotesDto
{
    public List<LoteListadoDto> Items { get; set; } = new List<LoteListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
