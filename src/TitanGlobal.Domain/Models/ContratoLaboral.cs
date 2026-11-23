// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ContratoLaboral
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string Modalidad { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public decimal SueldoBasico { get; set; }

    public string Cargo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public int? ContratoAnteriorId { get; set; }

    public DateOnly? FechaCierre { get; set; }

    public string? MotivoCierre { get; set; }

    public virtual ICollection<AdendaContrato> AdendasContrato { get; set; } = new List<AdendaContrato>();

    public virtual ContratoLaboral? ContratoAnterior { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual ICollection<ContratoLaboral> InverseContratoAnterior { get; set; } = new List<ContratoLaboral>();
}
