// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class EvaluacionProveedor
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string Periodo { get; set; } = string.Empty;

    public int PuntajeCumplimiento { get; set; }

    public int PuntajeCalidad { get; set; }

    public int PuntajePlazos { get; set; }

    public decimal PuntajeFinal { get; set; }

    public string Calificacion { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;
}
