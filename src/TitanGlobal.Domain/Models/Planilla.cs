// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Planilla
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int Anio { get; set; }

    public int Mes { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public int Empleados { get; set; }

    public int Incidencias { get; set; }

    public decimal TotalIngresos { get; set; }

    public decimal TotalDescuentos { get; set; }

    public decimal TotalNeto { get; set; }

    public decimal TotalAportesEmpleador { get; set; }

    public DateTime? FechaCalculo { get; set; }

    public string? CalculadoPor { get; set; }

    public string? AprobadoPor { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public DateOnly? FechaPago { get; set; }

    public virtual ICollection<BonoPlanilla> BonosPlanilla { get; set; } = new List<BonoPlanilla>();

    public virtual ICollection<DescuentoCuotaPrestamo> DescuentosCuotaPrestamo { get; set; } = new List<DescuentoCuotaPrestamo>();

    public virtual ICollection<PlanillaDetalle> Detalles { get; set; } = new List<PlanillaDetalle>();
}
