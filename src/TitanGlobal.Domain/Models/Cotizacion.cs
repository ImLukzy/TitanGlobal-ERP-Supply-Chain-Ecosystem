// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Cotizacion
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public DateTime FechaValidez { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaDecision { get; set; }

    public int? OrdenCompraId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DetalleCotizacion> Detalles { get; set; } = new List<DetalleCotizacion>();

    public virtual OrdenCompra? OrdenCompra { get; set; }
}
