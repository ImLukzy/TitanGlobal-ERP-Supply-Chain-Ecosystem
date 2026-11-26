// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class CuotaPrestamo
{
    public int Id { get; set; }

    public int PrestamoId { get; set; }

    public int NumeroCuota { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public decimal Monto { get; set; }

    public decimal MontoDescontado { get; set; }

    public string Estado { get; set; } = string.Empty;

    public virtual ICollection<DescuentoCuotaPrestamo> DescuentosCuotaPrestamo { get; set; } = new List<DescuentoCuotaPrestamo>();

    public virtual Prestamo Prestamo { get; set; } = null!;
}
