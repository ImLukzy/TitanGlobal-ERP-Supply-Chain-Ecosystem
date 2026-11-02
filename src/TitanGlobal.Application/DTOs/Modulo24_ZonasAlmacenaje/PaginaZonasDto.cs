namespace TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;

public class PaginaZonasDto
{
    public List<ZonaListadoDto> Items { get; set; } = new List<ZonaListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
