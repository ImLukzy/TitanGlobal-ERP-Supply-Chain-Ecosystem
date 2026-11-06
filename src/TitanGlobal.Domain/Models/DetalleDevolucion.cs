// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DetalleDevolucion
{
    public int Id { get; set; }

    public int DevolucionComercialId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public virtual DevolucionComercial DevolucionComercial { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
