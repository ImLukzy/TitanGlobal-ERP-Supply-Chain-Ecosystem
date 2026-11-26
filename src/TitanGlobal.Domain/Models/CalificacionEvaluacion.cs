// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class CalificacionEvaluacion
{
    public int Id { get; set; }

    public int EvaluacionId { get; set; }

    public int CompetenciaEvaluacionId { get; set; }

    public int Calificacion { get; set; }

    public virtual CompetenciaEvaluacion CompetenciaEvaluacion { get; set; } = null!;

    public virtual Evaluacion Evaluacion { get; set; } = null!;
}
