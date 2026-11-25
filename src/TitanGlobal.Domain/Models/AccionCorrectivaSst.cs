// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class AccionCorrectivaSst
{
    public int Id { get; set; }

    public int IncidenteSstId { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public string Responsable { get; set; } = string.Empty;

    public DateOnly FechaCompromiso { get; set; }

    public bool Completada { get; set; }

    public DateOnly? FechaCompletada { get; set; }

    public virtual IncidenteSst IncidenteSst { get; set; } = null!;
}
