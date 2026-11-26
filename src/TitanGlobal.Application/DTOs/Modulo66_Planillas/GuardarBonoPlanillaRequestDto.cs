namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class GuardarBonoPlanillaRequestDto
{
    public int? EmpleadoId { get; set; }

    public int? Anio { get; set; }

    public int? Mes { get; set; }

    public string? TipoPlanilla { get; set; }

    public string? Concepto { get; set; }

    public decimal? Monto { get; set; }
}
