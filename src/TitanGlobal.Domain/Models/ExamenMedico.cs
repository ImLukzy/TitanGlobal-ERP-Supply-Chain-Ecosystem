// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ExamenMedico
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public string Resultado { get; set; } = string.Empty;

    public string? Restricciones { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public string? Clinica { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;
}
