// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class RetornoLogistico
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string ReferenciaOrigen { get; set; } = string.Empty;

    public string Contraparte { get; set; } = string.Empty;

    public string PuntoRecogida { get; set; } = string.Empty;

    public string PuntoRecepcion { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Unidad { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public DateTime FechaProgramada { get; set; }

    public decimal CantidadProgramada { get; set; }

    public string? Observaciones { get; set; }

    public string Estado { get; set; } = string.Empty;

    public decimal? CantidadRecogida { get; set; }

    public decimal? CantidadRecibida { get; set; }

    public DateTime? FechaRecogida { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? ResponsableRecogida { get; set; }

    public string? ResponsableRecepcion { get; set; }

    public string? MotivoCancelacion { get; set; }
}
