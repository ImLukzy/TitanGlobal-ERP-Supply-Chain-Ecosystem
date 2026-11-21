// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class GuiaRemision
{
    public int Id { get; set; }

    public string Serie { get; set; } = string.Empty;

    public int Numero { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaInicioTraslado { get; set; }

    public string RemitenteNombre { get; set; } = string.Empty;

    public string RemitenteDocumento { get; set; } = string.Empty;

    public string DestinatarioNombre { get; set; } = string.Empty;

    public string DestinatarioDocumento { get; set; } = string.Empty;

    public string DireccionOrigen { get; set; } = string.Empty;

    public string DireccionDestino { get; set; } = string.Empty;

    public string MotivoTraslado { get; set; } = string.Empty;

    public string Transportista { get; set; } = string.Empty;

    public string Conductor { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public decimal PesoBrutoKg { get; set; }

    public string? Observaciones { get; set; }

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual ICollection<LineaGuiaRemision> Lineas { get; set; } = new List<LineaGuiaRemision>();
}
