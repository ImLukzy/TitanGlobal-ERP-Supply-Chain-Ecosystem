namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class BoletaPagoDto
{
    public string Empresa { get; set; } = string.Empty;

    public string NumeroBoleta { get; set; } = string.Empty;

    public string NumeroPlanilla { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public int Anio { get; set; }

    public int Mes { get; set; }

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public string Cargo { get; set; } = string.Empty;

    public string? CodigoPension { get; set; }

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

    public decimal DescuentoPension { get; set; }

    public decimal DescuentoQuinta { get; set; }

    public decimal DescuentoPrestamos { get; set; }

    public decimal TotalDescuentos { get; set; }

    public decimal AporteEssalud { get; set; }

    public decimal Neto { get; set; }

    public DateOnly? FechaPago { get; set; }
}
