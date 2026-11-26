// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DetalleListaMaterial
{
    public int Id { get; set; }

    public int ListaMaterialId { get; set; }

    public int ComponenteProductoId { get; set; }

    public decimal Cantidad { get; set; }

    public string UnidadMedida { get; set; } = string.Empty;

    public virtual Producto ComponenteProducto { get; set; } = null!;

    public virtual ListaMateriales ListaMaterial { get; set; } = null!;
}
