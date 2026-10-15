namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;

public class PaginaClientesSegmentoDto
{
    public List<ClienteClasificadoDto> Items { get; set; } = new List<ClienteClasificadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
