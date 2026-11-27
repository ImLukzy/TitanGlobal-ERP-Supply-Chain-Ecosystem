// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class AcuerdoAbastecimiento
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ProveedorId { get; set; }

    public string? Descripcion { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoMaximo { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<LineaAcuerdoAbastecimiento> LineasAcuerdoAbastecimiento { get; set; } = new List<LineaAcuerdoAbastecimiento>();

    public virtual Proveedor Proveedor { get; set; } = null!;
}
