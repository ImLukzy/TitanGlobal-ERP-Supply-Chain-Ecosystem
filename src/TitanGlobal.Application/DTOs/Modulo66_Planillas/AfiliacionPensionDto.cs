namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class AfiliacionPensionDto
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public string CodigoPension { get; set; } = string.Empty;

    public string Sistema { get; set; } = string.Empty;

    public string? Cuspp { get; set; }

    public DateOnly FechaAfiliacion { get; set; }
}
