// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class BonoPlanilla
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public string TipoPlanilla { get; set; } = string.Empty;

    public string Concepto { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public int? PlanillaId { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual Planilla? Planilla { get; set; }
}
