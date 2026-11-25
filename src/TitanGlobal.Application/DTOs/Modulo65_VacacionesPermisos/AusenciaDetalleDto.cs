namespace TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;

public class AusenciaDetalleDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public int Dias { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly FechaSolicitud { get; set; }

    public string? Motivo { get; set; }

    public string? DecididoPor { get; set; }

    public DateTime? FechaDecision { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateOnly? FechaCancelacion { get; set; }
}
