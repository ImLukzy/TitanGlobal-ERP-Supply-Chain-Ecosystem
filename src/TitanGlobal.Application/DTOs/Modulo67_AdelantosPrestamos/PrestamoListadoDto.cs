namespace TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;

public class PrestamoListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public int NumeroCuotas { get; set; }

    public DateOnly FechaOtorgamiento { get; set; }

    public decimal Saldo { get; set; }

    public string Estado { get; set; } = string.Empty;
}
