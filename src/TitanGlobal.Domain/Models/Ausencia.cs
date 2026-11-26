// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Ausencia
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public int Dias { get; set; }

    public string? Motivo { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly FechaSolicitud { get; set; }

    public string? DecididoPor { get; set; }

    public DateTime? FechaDecision { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateOnly? FechaCancelacion { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;
}
