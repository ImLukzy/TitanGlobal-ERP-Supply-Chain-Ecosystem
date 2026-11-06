// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class TasaImpuesto
{
    public int Id { get; set; }

    public int ImpuestoId { get; set; }

    public decimal Porcentaje { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activo { get; set; }

    public string? Observacion { get; set; }

    public virtual Impuesto Impuesto { get; set; } = null!;
}
