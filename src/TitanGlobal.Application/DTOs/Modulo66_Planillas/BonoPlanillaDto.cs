namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class BonoPlanillaDto
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public int Anio { get; set; }

    public int Mes { get; set; }

    public string TipoPlanilla { get; set; } = string.Empty;

    public string Concepto { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public int? PlanillaId { get; set; }

    public string? PlanillaNumero { get; set; }
}
