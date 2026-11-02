namespace TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;

public class PaginaSolicitudesDto
{
    public List<SolicitudListadoDto> Items { get; set; } = new List<SolicitudListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
