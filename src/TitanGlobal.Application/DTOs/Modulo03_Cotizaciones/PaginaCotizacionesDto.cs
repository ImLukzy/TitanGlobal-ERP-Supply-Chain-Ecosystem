namespace TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;

public class PaginaCotizacionesDto
{
    public List<CotizacionListadoDto> Items { get; set; } = new List<CotizacionListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
