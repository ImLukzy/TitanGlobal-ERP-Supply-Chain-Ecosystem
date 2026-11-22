namespace TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;

public class PaginaSiniestrosDto
{
    public List<SiniestroListadoDto> Items { get; set; } = new List<SiniestroListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
