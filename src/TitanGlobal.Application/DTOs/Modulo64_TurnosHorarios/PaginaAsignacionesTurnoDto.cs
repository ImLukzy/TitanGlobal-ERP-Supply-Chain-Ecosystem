namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class PaginaAsignacionesTurnoDto
{
    public List<AsignacionTurnoListadoDto> Items { get; set; } = new List<AsignacionTurnoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
