// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Rol
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EsSistema { get; set; }

    public virtual ICollection<PermisoRol> PermisosRoles { get; set; } = new List<PermisoRol>();

    public virtual ICollection<UsuarioRol> Usuarios { get; set; } = new List<UsuarioRol>();
}
