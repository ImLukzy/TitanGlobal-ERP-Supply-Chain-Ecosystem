// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class EntregaDespacho
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string ReferenciaGuia { get; set; } = string.Empty;

    public string Destinatario { get; set; } = string.Empty;

    public string DireccionDestino { get; set; } = string.Empty;

    public DateTime FechaProgramada { get; set; }

    public string? Observaciones { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaSalida { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? Receptor { get; set; }

    public string? DocumentoReceptor { get; set; }

    public string? ReferenciaConstancia { get; set; }

    public string? ObservacionesEntrega { get; set; }

    public string? MotivoCancelacion { get; set; }

    public virtual ICollection<EventoEntrega> Eventos { get; set; } = new List<EventoEntrega>();
}
