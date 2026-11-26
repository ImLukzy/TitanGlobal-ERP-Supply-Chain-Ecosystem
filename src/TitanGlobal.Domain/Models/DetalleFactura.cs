// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DetalleFactura
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public int ProductoId { get; set; }

    public int? ImpuestoId { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    public decimal PorcentajeImpuesto { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ImporteDescuento { get; set; }

    public decimal BaseImponible { get; set; }

    public decimal ImporteImpuesto { get; set; }

    public decimal Total { get; set; }

    public virtual ICollection<DetalleNotaCreditoDebito> DetallesNotaCreditoDebito { get; set; } = new List<DetalleNotaCreditoDebito>();

    public virtual Factura Factura { get; set; } = null!;

    public virtual Impuesto? Impuesto { get; set; }

    public virtual Producto Producto { get; set; } = null!;
}
