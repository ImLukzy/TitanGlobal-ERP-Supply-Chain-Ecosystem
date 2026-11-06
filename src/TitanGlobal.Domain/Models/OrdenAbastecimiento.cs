// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class OrdenAbastecimiento
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ProveedorId { get; set; }

    public int? SolicitudCompraId { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaEntregaEstimada { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public string? Observaciones { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual ICollection<DetalleOrdenAbastecimiento> Detalles { get; set; } = new List<DetalleOrdenAbastecimiento>();

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual SolicitudCompra? SolicitudCompra { get; set; }
}
