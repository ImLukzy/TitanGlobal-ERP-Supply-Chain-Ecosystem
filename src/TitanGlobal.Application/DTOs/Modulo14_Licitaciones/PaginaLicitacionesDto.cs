namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class PaginaLicitacionesDto
{
    public List<LicitacionListadoDto> Items { get; set; } = new List<LicitacionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
