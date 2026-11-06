// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DevolucionComercial
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public int OrdenCompraId { get; set; }

    public int? ReclamoPostventaId { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public DateTime? FechaRechazo { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DetalleDevolucion> Detalles { get; set; } = new List<DetalleDevolucion>();

    public virtual OrdenCompra OrdenCompra { get; set; } = null!;

    public virtual ReclamoPostventa? ReclamoPostventa { get; set; }
}
