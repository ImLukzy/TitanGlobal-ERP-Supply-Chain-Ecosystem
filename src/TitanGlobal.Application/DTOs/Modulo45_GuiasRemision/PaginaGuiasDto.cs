namespace TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;

public class PaginaGuiasDto
{
    public List<GuiaListadoDto> Items { get; set; } = new();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
