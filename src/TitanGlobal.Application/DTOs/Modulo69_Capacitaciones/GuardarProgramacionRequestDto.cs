namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class GuardarProgramacionRequestDto
{
    public int? CursoId { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public int? Cupo { get; set; }

    public string? Lugar { get; set; }
}
