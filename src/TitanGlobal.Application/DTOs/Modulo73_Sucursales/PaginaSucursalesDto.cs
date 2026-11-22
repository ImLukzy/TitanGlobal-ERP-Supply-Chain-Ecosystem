namespace TitanGlobal.Application.DTOs.Modulo73_Sucursales;

public class PaginaSucursalesDto
{
    public List<SucursalListadoDto> Items { get; set; } = new List<SucursalListadoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
