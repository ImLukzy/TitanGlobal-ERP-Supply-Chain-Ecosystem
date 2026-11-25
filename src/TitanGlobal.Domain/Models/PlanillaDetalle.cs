// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class PlanillaDetalle
{
    public int Id { get; set; }

    public int PlanillaId { get; set; }

    public int EmpleadoId { get; set; }

    public int? ContratoLaboralId { get; set; }

    public string Cargo { get; set; } = string.Empty;

    public decimal SueldoBasico { get; set; }

    public int DiasContrato { get; set; }

    public int DiasSinGoce { get; set; }

    public int DiasFalta { get; set; }

    public int DiasLaborados { get; set; }

    public decimal IngresoBasico { get; set; }

    public int MinutosExtra { get; set; }

    public decimal IngresoHorasExtra { get; set; }

    public decimal IngresoBonos { get; set; }

    public decimal TotalIngresos { get; set; }

    public string? CodigoPension { get; set; }

    public decimal DescuentoPension { get; set; }

    public decimal DescuentoQuinta { get; set; }

    public decimal DescuentoPrestamos { get; set; }

    public decimal TotalDescuentos { get; set; }

    public decimal Neto { get; set; }

    public decimal AporteEssalud { get; set; }

    public string? Incidencia { get; set; }

    public string? NumeroBoleta { get; set; }

    public virtual ContratoLaboral? ContratoLaboral { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual Planilla Planilla { get; set; } = null!;
}
