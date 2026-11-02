namespace TitanGlobal.Application.DTOs.Modulo23_Almacenes;

public class PaginaAlmacenesDto
{
    public List<AlmacenListadoDto> Items { get; set; } = new List<AlmacenListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
