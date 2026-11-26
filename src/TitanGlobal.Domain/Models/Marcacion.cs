// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Marcacion
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public DateTime Instante { get; set; }

    public string Origen { get; set; } = string.Empty;

    public string? Dispositivo { get; set; }

    public string? NumeroRegistro { get; set; }

    public string? Observacion { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;
}
