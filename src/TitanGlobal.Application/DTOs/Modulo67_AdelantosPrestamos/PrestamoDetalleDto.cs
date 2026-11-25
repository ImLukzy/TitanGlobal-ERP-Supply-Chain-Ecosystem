namespace TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;

public class PrestamoDetalleDto
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

    public int PrimerAnio { get; set; }

    public int PrimerMes { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public decimal Saldo { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public List<CuotaPrestamoDto> Cuotas { get; set; } = new List<CuotaPrestamoDto>();
}
