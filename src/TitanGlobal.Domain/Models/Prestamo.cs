// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Prestamo
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public int NumeroCuotas { get; set; }

    public DateOnly FechaOtorgamiento { get; set; }

    public int PrimerAnio { get; set; }

    public int PrimerMes { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal Saldo { get; set; }

    public DateOnly? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual ICollection<CuotaPrestamo> CuotasPrestamo { get; set; } = new List<CuotaPrestamo>();

    public virtual Empleado Empleado { get; set; } = null!;
}
