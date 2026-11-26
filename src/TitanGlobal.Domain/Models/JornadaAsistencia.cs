// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class JornadaAsistencia
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public DateOnly Fecha { get; set; }

    public int? TurnoId { get; set; }

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

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual Turno? Turno { get; set; }
}
