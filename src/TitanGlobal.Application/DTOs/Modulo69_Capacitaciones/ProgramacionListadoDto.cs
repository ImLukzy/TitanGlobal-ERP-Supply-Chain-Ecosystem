namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class ProgramacionListadoDto
{
    public int Id { get; set; }

    public int CursoId { get; set; }

    public string CursoCodigo { get; set; } = string.Empty;

    public string CursoNombre { get; set; } = string.Empty;

    public decimal Horas { get; set; }

    public int Anio { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public int Cupo { get; set; }

    public int Inscritos { get; set; }

    public int CupoLibre { get; set; }

    public string? Lugar { get; set; }

    public string Estado { get; set; } = string.Empty;
}
