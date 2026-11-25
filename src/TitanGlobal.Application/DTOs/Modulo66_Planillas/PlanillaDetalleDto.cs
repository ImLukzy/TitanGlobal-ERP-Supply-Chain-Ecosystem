namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class PlanillaDetalleDto
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
}
