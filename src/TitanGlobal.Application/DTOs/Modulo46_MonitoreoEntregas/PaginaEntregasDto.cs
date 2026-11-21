namespace TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;

public class PaginaEntregasDto
{
    public List<EntregaListadoDto> Items { get; set; } = new();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
