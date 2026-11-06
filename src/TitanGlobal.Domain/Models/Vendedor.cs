// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Vendedor
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Email { get; set; }

    public decimal PorcentajeComision { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<AsignacionVenta> AsignacionesVentas { get; set; } = new List<AsignacionVenta>();

    public virtual ICollection<LiquidacionComision> LiquidacionesComisiones { get; set; } = new List<LiquidacionComision>();
}
