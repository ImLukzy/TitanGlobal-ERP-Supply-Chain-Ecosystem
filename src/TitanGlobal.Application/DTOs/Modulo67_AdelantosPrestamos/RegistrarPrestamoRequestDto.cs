namespace TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;

public class RegistrarPrestamoRequestDto
{
    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public decimal? Monto { get; set; }

    public int? NumeroCuotas { get; set; }

    public DateOnly? FechaOtorgamiento { get; set; }

    public int? PrimerAnio { get; set; }

    public int? PrimerMes { get; set; }

    public string? Motivo { get; set; }
}
