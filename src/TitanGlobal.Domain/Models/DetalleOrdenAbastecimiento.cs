// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DetalleOrdenAbastecimiento
{
    public int Id { get; set; }

    public int OrdenAbastecimientoId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public virtual OrdenAbastecimiento OrdenAbastecimiento { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
