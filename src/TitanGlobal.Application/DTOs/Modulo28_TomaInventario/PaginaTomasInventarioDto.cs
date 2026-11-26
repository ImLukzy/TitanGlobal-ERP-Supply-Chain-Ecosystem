namespace TitanGlobal.Application.DTOs.Modulo28_TomaInventario;

public class PaginaTomasInventarioDto
{
    public List<TomaInventarioListadoDto> Items { get; set; } = new List<TomaInventarioListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
