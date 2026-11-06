// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ItemListaPrecios
{
    public int Id { get; set; }

    public int ListaPreciosId { get; set; }

    public int ProductoId { get; set; }

    public decimal Precio { get; set; }

    public virtual ListaPrecios ListaPrecios { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
