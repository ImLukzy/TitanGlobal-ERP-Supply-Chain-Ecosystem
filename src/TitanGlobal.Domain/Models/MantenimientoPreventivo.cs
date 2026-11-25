// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class MantenimientoPreventivo
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int CentroTrabajoId { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public DateOnly FechaProgramada { get; set; }

    public decimal DuracionHorasPlanificada { get; set; }

    public int? FrecuenciaDias { get; set; }

    public string? Responsable { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaInicioReal { get; set; }

    public DateTime? FechaFinReal { get; set; }

    public decimal? HorasReales { get; set; }

    public string? Observaciones { get; set; }

    public string? MotivoCancelacion { get; set; }

    public virtual CentroTrabajo CentroTrabajo { get; set; } = null!;
}
