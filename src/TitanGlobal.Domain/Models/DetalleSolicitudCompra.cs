// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DetalleSolicitudCompra
{
    public int Id { get; set; }

    public int SolicitudCompraId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioEstimado { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual SolicitudCompra SolicitudCompra { get; set; } = null!;
}
