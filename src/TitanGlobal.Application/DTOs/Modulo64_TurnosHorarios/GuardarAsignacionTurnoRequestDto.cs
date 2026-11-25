namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class GuardarAsignacionTurnoRequestDto
{
    public int? EmpleadoId { get; set; }

    public int? TurnoId { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }
}
