// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class PermisoRol
{
    public int Id { get; set; }

    public int RolId { get; set; }

    public int ModuloNumero { get; set; }

    public bool PuedeLeer { get; set; }

    public bool PuedeEscribir { get; set; }

    public virtual Rol Rol { get; set; } = null!;
}
