namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.Dtos;

public class PaginaAlmacenesDto
{
    public List<AlmacenListadoDto> Items { get; set; } = new List<AlmacenListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
