// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Empleado
{
    public int Id { get; set; }

    public string Legajo { get; set; } = string.Empty;

    public string TipoDocumento { get; set; } = string.Empty;

    public string NumeroDocumento { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public DateOnly FechaNacimiento { get; set; }

    public DateOnly FechaIngreso { get; set; }

    public string Cargo { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly? FechaCese { get; set; }

    public string? MotivoCese { get; set; }

    public virtual AfiliacionPension? AfiliacionPension { get; set; }

    public virtual ICollection<AsignacionTurno> AsignacionesTurno { get; set; } = new List<AsignacionTurno>();

    public virtual ICollection<Ausencia> Ausencias { get; set; } = new List<Ausencia>();

    public virtual ICollection<BonoPlanilla> BonosPlanilla { get; set; } = new List<BonoPlanilla>();

    public virtual ICollection<ContratoLaboral> ContratosLaborales { get; set; } = new List<ContratoLaboral>();

    public virtual ICollection<Evaluacion> EvaluacionesEmpleado { get; set; } = new List<Evaluacion>();

    public virtual ICollection<Evaluacion> EvaluacionesEvaluador { get; set; } = new List<Evaluacion>();

    public virtual ICollection<InscripcionCurso> InscripcionesCurso { get; set; } = new List<InscripcionCurso>();

    public virtual ICollection<JornadaAsistencia> JornadasAsistencia { get; set; } = new List<JornadaAsistencia>();

    public virtual ICollection<Marcacion> Marcaciones { get; set; } = new List<Marcacion>();

    public virtual ICollection<PlanillaDetalle> PlanillaDetalles { get; set; } = new List<PlanillaDetalle>();

    public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
