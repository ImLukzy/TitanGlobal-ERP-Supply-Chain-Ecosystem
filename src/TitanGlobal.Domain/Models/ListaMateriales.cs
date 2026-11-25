// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ListaMateriales
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public int Version { get; set; }

    public string Estado { get; set; } = string.Empty;

    public virtual ICollection<DetalleListaMaterial> DetallesListaMaterial { get; set; } = new List<DetalleListaMaterial>();

    public virtual Producto Producto { get; set; } = null!;
}
