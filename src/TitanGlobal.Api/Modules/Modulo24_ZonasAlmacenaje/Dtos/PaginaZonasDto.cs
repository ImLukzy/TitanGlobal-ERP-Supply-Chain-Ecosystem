namespace TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.Dtos;

public class PaginaZonasDto
{
    public List<ZonaListadoDto> Items { get; set; } = new List<ZonaListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
