namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class PaginaReglasReabastecimientoDto
{
    public List<ReglaReabastecimientoListadoDto> Items { get; set; } = new List<ReglaReabastecimientoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
