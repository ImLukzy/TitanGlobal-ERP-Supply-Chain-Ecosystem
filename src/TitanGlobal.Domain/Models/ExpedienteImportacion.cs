// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ExpedienteImportacion
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int OrdenAbastecimientoId { get; set; }

    public string Incoterm { get; set; } = string.Empty;

    public string MedioTransporte { get; set; } = string.Empty;

    public string PaisOrigen { get; set; } = string.Empty;

    public string LugarOrigen { get; set; } = string.Empty;

    public string LugarDestino { get; set; } = string.Empty;

    public string? DocumentoTransporte { get; set; }

    public DateTime? FechaEmbarque { get; set; }

    public DateTime FechaLlegadaEstimada { get; set; }

    public DateTime? FechaLlegadaReal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? MotivoCancelacion { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual DeclaracionAduana? DeclaracionAduana { get; set; }

    public virtual OrdenAbastecimiento OrdenAbastecimiento { get; set; } = null!;
}
