// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class OrdenProduccion
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ProductoId { get; set; }

    public int ListaMaterialesId { get; set; }

    public int RutaProduccionId { get; set; }

    public int CantidadPlanificada { get; set; }

    public int? CantidadProducida { get; set; }

    public DateOnly FechaInicioPlanificada { get; set; }

    public DateOnly FechaFinPlanificada { get; set; }

    public DateTime? FechaInicioReal { get; set; }

    public DateTime? FechaFinReal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? MotivoCancelacion { get; set; }

    public virtual ICollection<InspeccionProductoTerminado> InspeccionesProductoTerminado { get; set; } = new List<InspeccionProductoTerminado>();

    public virtual ListaMateriales ListaMateriales { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;

    public virtual RutaProduccion RutaProduccion { get; set; } = null!;
}
