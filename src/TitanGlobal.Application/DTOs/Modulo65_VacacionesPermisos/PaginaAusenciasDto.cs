namespace TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;

public class PaginaAusenciasDto
{
    public List<AusenciaListadoDto> Items { get; set; } = new List<AusenciaListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
