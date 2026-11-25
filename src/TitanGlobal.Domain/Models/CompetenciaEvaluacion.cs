// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class CompetenciaEvaluacion
{
    public int Id { get; set; }

    public int CicloEvaluacionId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public decimal Peso { get; set; }

    public virtual ICollection<CalificacionEvaluacion> CalificacionesEvaluacion { get; set; } = new List<CalificacionEvaluacion>();

    public virtual CicloEvaluacion CicloEvaluacion { get; set; } = null!;
}
