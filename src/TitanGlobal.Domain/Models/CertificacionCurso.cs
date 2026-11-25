// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class CertificacionCurso
{
    public int Id { get; set; }

    public int InscripcionCursoId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public DateOnly FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public virtual InscripcionCurso InscripcionCurso { get; set; } = null!;
}
