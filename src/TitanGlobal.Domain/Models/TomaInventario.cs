// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class TomaInventario
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public DateTime FechaToma { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string Responsable { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public virtual Almacen Almacen { get; set; } = null!;

    public virtual ICollection<DetalleTomaInventario> DetallesTomaInventario { get; set; } = new List<DetalleTomaInventario>();
}
