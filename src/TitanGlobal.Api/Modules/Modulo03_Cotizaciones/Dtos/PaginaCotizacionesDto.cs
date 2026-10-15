namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;

public class PaginaCotizacionesDto
{
    public List<CotizacionListadoDto> Items { get; set; } = new List<CotizacionListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
