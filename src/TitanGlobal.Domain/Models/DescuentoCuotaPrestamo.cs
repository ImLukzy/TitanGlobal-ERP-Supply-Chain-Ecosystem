// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DescuentoCuotaPrestamo
{
    public int Id { get; set; }

    public int CuotaPrestamoId { get; set; }

    public int PlanillaId { get; set; }

    public decimal Monto { get; set; }

    public virtual CuotaPrestamo CuotaPrestamo { get; set; } = null!;

    public virtual Planilla Planilla { get; set; } = null!;
}
