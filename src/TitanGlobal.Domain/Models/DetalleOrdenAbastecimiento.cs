namespace TitanGlobal.Domain.Models;

public partial class DetalleOrdenAbastecimiento
{
    public int Id { get; set; }

    public int OrdenAbastecimientoId { get; set; }

    public virtual OrdenAbastecimiento OrdenAbastecimiento { get; set; } = null!;

    public int ProductoId { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}
