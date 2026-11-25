// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ProgramacionCurso
{
    public int Id { get; set; }

    public int CursoId { get; set; }

    public int Anio { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public int Cupo { get; set; }

    public string? Lugar { get; set; }

    public string Estado { get; set; } = string.Empty;

    public virtual Curso Curso { get; set; } = null!;

    public virtual ICollection<InscripcionCurso> InscripcionesCurso { get; set; } = new List<InscripcionCurso>();
}
