// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class TransportistaTercero
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string DocumentoFiscal { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string TipoServicio { get; set; } = string.Empty;

    public string? Contacto { get; set; }

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string? Observaciones { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<FleteTercero> Fletes { get; set; } = new List<FleteTercero>();
}
