// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class DeclaracionAduana
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ExpedienteImportacionId { get; set; }

    public int AgenteAduaneroId { get; set; }

    public string Regimen { get; set; } = string.Empty;

    public decimal ValorAduanero { get; set; }

    public decimal TasaArancelaria { get; set; }

    public decimal DerechosArancel { get; set; }

    public string? Canal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaNumeracion { get; set; }

    public DateTime? FechaLevante { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual AgenteAduanero AgenteAduanero { get; set; } = null!;

    public virtual ExpedienteImportacion ExpedienteImportacion { get; set; } = null!;
}
