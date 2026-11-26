// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class InspeccionProductoTerminado
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int OrdenProduccionId { get; set; }

    public DateOnly FechaInspeccion { get; set; }

    public int CantidadInspeccionada { get; set; }

    public decimal ToleranciaNoConformesPorcentaje { get; set; }

    public string Inspector { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public int? CantidadNoConforme { get; set; }

    public DateTime? FechaDictamen { get; set; }

    public string? Observaciones { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual OrdenProduccion OrdenProduccion { get; set; } = null!;
}
