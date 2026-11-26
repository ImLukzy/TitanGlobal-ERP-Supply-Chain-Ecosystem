// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ConsumoMaterial
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int OrdenProduccionId { get; set; }

    public int ComponenteProductoId { get; set; }

    public decimal Cantidad { get; set; }

    public string UnidadMedida { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public string? Responsable { get; set; }

    public string? Observaciones { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual Producto ComponenteProducto { get; set; } = null!;

    public virtual OrdenProduccion OrdenProduccion { get; set; } = null!;
}
