// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DetalleTomaInventario
{
    public int Id { get; set; }

    public int TomaInventarioId { get; set; }

    public int ProductoId { get; set; }

    public int StockSistema { get; set; }

    public int CantidadContada { get; set; }

    public int Diferencia { get; set; }

    public string? Observacion { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual TomaInventario TomaInventario { get; set; } = null!;
}
