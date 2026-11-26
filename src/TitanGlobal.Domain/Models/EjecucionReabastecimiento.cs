// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class EjecucionReabastecimiento
{
    public int Id { get; set; }

    public int ReglaReabastecimientoId { get; set; }

    public DateTime FechaEjecucion { get; set; }

    public int StockActual { get; set; }

    public int CantidadSugerida { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int? SolicitudCompraId { get; set; }

    public string? Observacion { get; set; }

    public virtual ReglaReabastecimiento ReglaReabastecimiento { get; set; } = null!;

    public virtual SolicitudCompra? SolicitudCompra { get; set; }
}
