namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class AsignacionTurnoListadoDto
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public int TurnoId { get; set; }

    public string TurnoCodigo { get; set; } = string.Empty;

    public string TurnoNombre { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public string Estado { get; set; } = string.Empty;
}
