// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class AfiliacionPension
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public int ParametroPensionId { get; set; }

    public string? Cuspp { get; set; }

    public DateOnly FechaAfiliacion { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual ParametroPension ParametroPension { get; set; } = null!;
}
