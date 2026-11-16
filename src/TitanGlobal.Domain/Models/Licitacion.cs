// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Licitacion
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal PresupuestoReferencial { get; set; }

    public DateTime FechaCierre { get; set; }

    public int? ProveedorAdjudicadoId { get; set; }

    public decimal? MontoAdjudicado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<OfertaLicitacion> OfertasLicitacion { get; set; } = new List<OfertaLicitacion>();

    public virtual Proveedor? ProveedorAdjudicado { get; set; }
}
