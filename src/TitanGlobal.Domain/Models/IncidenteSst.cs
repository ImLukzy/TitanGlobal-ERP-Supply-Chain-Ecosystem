// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class IncidenteSst
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Severidad { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public string? Lugar { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int DiasBaja { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly? FechaCierre { get; set; }

    public virtual ICollection<AccionCorrectivaSst> AccionesCorrectivasSst { get; set; } = new List<AccionCorrectivaSst>();

    public virtual Empleado Empleado { get; set; } = null!;
}
