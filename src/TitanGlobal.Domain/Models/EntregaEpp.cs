// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class EntregaEpp
{
    public int Id { get; set; }

    public int EquipoProteccionId { get; set; }

    public int EmpleadoId { get; set; }

    public DateOnly FechaEntrega { get; set; }

    public int Cantidad { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual EquipoProteccion EquipoProteccion { get; set; } = null!;
}
