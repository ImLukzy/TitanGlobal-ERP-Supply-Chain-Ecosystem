namespace TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;

public class PaginaSegmentosDto
{
    public List<SegmentoListadoDto> Items { get; set; } = new List<SegmentoListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
