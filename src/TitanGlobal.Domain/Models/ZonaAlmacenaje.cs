// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ZonaAlmacenaje
{
    public int Id { get; set; }

    public int AlmacenId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Temperatura { get; set; } = string.Empty;

    public decimal CapacidadM3 { get; set; }

    public bool Activa { get; set; }

    public virtual Almacen Almacen { get; set; } = null!;
}
