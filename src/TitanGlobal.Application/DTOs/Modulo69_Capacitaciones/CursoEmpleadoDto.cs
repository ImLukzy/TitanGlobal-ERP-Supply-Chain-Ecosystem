namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class CursoEmpleadoDto
{
    public int InscripcionId { get; set; }

    public int ProgramacionId { get; set; }

    public string CursoCodigo { get; set; } = string.Empty;

    public string CursoNombre { get; set; } = string.Empty;

    public decimal Horas { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public string Estado { get; set; } = string.Empty;

    public decimal? Nota { get; set; }
}
