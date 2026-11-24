// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Turno
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public TimeOnly HoraEntrada { get; set; }

    public TimeOnly HoraSalida { get; set; }

    public bool CruzaMedianoche { get; set; }

    public int ToleranciaMinutos { get; set; }

    public string? DiasLaborables { get; set; }

    public int? DiasTrabajo { get; set; }

    public int? DiasDescanso { get; set; }

    public DateOnly? FechaInicioCiclo { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<AsignacionTurno> AsignacionesTurno { get; set; } = new List<AsignacionTurno>();
}
