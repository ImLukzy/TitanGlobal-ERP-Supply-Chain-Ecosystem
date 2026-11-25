// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Proveedor
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<AcuerdoAbastecimiento> AcuerdosAbastecimiento { get; set; } = new List<AcuerdoAbastecimiento>();

    public virtual ICollection<EvaluacionProveedor> EvaluacionesProveedor { get; set; } = new List<EvaluacionProveedor>();

    public virtual ICollection<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = new List<OrdenAbastecimiento>();

    public virtual ICollection<SolicitudCompra> SolicitudesCompra { get; set; } = new List<SolicitudCompra>();
}
