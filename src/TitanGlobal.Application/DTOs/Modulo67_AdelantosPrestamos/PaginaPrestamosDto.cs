namespace TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;

public class PaginaPrestamosDto
{
    public List<PrestamoListadoDto> Items { get; set; } = new List<PrestamoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
