namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;

public class PaginaSegmentosDto
{
    public List<SegmentoListadoDto> Items { get; set; } = new List<SegmentoListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
