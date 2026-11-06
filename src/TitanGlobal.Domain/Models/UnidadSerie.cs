// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class UnidadSerie
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public int? LoteId { get; set; }

    public string NumeroSerie { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public virtual Lote? Lote { get; set; }

    public virtual Producto Producto { get; set; } = null!;
}
