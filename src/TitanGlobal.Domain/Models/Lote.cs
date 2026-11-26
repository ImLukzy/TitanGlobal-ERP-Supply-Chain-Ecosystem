// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Lote
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public DateTime? FechaFabricacion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int Cantidad { get; set; }

    public string Estado { get; set; } = string.Empty;

    public virtual Producto Producto { get; set; } = null!;

    public virtual ICollection<UnidadSerie> UnidadesSerie { get; set; } = new List<UnidadSerie>();
}
