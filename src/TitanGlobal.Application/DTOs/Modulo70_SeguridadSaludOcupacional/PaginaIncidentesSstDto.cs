namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class PaginaIncidentesSstDto
{
    public List<IncidenteSstListadoDto> Items { get; set; } = new List<IncidenteSstListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
