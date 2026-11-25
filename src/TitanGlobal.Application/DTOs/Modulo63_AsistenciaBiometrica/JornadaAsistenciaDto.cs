namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class JornadaAsistenciaDto
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public int? TurnoId { get; set; }

    public string? TurnoCodigo { get; set; }

    public TimeOnly? HoraEntradaProgramada { get; set; }

    public TimeOnly? HoraSalidaProgramada { get; set; }

    public int ToleranciaMinutos { get; set; }

    public DateTime? Entrada { get; set; }

    public DateTime? Salida { get; set; }

    public int MinutosTrabajados { get; set; }

    public int MinutosTardanza { get; set; }

    public int MinutosExtra { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string EstadoHorasExtra { get; set; } = string.Empty;

    public string? DecididoPor { get; set; }

    public DateTime? FechaDecision { get; set; }

    public string? MotivoRechazo { get; set; }
}
