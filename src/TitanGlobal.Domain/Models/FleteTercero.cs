// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class FleteTercero
{
    public int Id { get; set; }

    public int TransportistaTerceroId { get; set; }

    public string Referencia { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Origen { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal Importe { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public virtual TransportistaTercero TransportistaTercero { get; set; } = null!;
}
