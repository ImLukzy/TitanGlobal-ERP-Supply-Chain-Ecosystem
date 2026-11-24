namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class PaginaSugerenciasReabastecimientoDto
{
    public List<EjecucionReabastecimientoItemDto> Items { get; set; } = new List<EjecucionReabastecimientoItemDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
