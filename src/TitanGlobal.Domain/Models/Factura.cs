// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Factura
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public int? OrdenCompraId { get; set; }

    public string TipoComprobante { get; set; } = string.Empty;

    public string Serie { get; set; } = string.Empty;

    public int Numero { get; set; }

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal TotalDescuento { get; set; }

    public decimal TotalImpuesto { get; set; }

    public decimal Total { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DetalleFactura> DetallesFactura { get; set; } = new List<DetalleFactura>();

    public virtual OrdenCompra? OrdenCompra { get; set; }
}
