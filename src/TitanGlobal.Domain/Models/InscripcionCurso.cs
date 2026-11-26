// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class InscripcionCurso
{
    public int Id { get; set; }

    public int ProgramacionCursoId { get; set; }

    public int EmpleadoId { get; set; }

    public string Estado { get; set; } = string.Empty;

    public decimal? Nota { get; set; }

    public DateOnly FechaInscripcion { get; set; }

    public virtual CertificacionCurso? CertificacionCurso { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual ProgramacionCurso ProgramacionCurso { get; set; } = null!;
}
