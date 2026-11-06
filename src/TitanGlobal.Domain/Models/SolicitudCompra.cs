// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class SolicitudCompra
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public int? ProveedorId { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaDecision { get; set; }

    public virtual ICollection<DetalleSolicitudCompra> Detalles { get; set; } = new List<DetalleSolicitudCompra>();

    public virtual ICollection<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = new List<OrdenAbastecimiento>();

    public virtual Proveedor? Proveedor { get; set; }
}
