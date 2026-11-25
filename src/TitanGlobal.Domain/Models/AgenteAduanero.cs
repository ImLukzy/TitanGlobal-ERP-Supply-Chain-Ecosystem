// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class AgenteAduanero
{
    public int Id { get; set; }

    public string Ruc { get; set; } = string.Empty;

    public string RazonSocial { get; set; } = string.Empty;

    public string NumeroRegistro { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<DeclaracionAduana> DeclaracionesAduana { get; set; } = new List<DeclaracionAduana>();
}
