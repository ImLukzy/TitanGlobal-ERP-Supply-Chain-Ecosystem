// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class LiquidacionComision
{
    public int Id { get; set; }

    public int VendedorId { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public int Ordenes { get; set; }

    public decimal Ventas { get; set; }

    public decimal Porcentaje { get; set; }

    public decimal Comision { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaCalculo { get; set; }

    public DateTime? FechaPago { get; set; }

    public virtual Vendedor Vendedor { get; set; } = null!;
}
