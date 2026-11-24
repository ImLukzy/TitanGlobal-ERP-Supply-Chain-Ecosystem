// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class CentroCosto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int? ResponsableEmpleadoId { get; set; }

    public int? CentroCostoPadreId { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaDesactivacion { get; set; }

    public virtual CentroCosto? CentroCostoPadre { get; set; }

    public virtual ICollection<CentroCosto> InverseCentroCostoPadre { get; set; } = new List<CentroCosto>();

    public virtual Empleado? ResponsableEmpleado { get; set; }
}
