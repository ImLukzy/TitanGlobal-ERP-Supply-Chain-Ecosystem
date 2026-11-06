// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class AsignacionVenta
{
    public int Id { get; set; }

    public int OrdenCompraId { get; set; }

    public int VendedorId { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public virtual OrdenCompra OrdenCompra { get; set; } = null!;

    public virtual Vendedor Vendedor { get; set; } = null!;
}
