// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Evaluacion
{
    public int Id { get; set; }

    public int CicloEvaluacionId { get; set; }

    public int EmpleadoId { get; set; }

    public int EvaluadorId { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? Comentario { get; set; }

    public decimal? PuntajeFinal { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public DateTime? FechaCierre { get; set; }

    public virtual ICollection<CalificacionEvaluacion> CalificacionesEvaluacion { get; set; } = new List<CalificacionEvaluacion>();

    public virtual CicloEvaluacion CicloEvaluacion { get; set; } = null!;

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual Empleado Evaluador { get; set; } = null!;
}
