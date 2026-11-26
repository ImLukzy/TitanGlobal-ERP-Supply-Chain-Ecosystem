// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ConsolidacionCarga
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public DateTime? FechaConfirmacion { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public virtual ICollection<LineaConsolidacionCarga> Lineas { get; set; } = new List<LineaConsolidacionCarga>();
}
