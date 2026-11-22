namespace TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;

public class PaginaRetornosDto
{
    public List<RetornoListadoDto> Items { get; set; } = new List<RetornoListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
