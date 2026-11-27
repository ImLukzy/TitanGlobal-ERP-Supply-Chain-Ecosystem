// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class OfertaLicitacion
{
    public int Id { get; set; }

    public int LicitacionId { get; set; }

    public int ProveedorId { get; set; }

    public decimal Monto { get; set; }

    public int PlazoEntregaDias { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Licitacion Licitacion { get; set; } = null!;

    public virtual Proveedor Proveedor { get; set; } = null!;
}
