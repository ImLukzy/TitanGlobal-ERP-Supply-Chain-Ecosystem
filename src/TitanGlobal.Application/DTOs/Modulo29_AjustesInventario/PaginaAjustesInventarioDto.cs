namespace TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;

public class PaginaAjustesInventarioDto
{
    public List<AjusteInventarioItemDto> Items { get; set; } = new List<AjusteInventarioItemDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
