namespace TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;

public class RegistrarAusenciaRequestDto
{
    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public string? Motivo { get; set; }
}
