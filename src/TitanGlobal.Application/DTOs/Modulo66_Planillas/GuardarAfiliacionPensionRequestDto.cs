namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class GuardarAfiliacionPensionRequestDto
{
    public string? CodigoPension { get; set; }

    public string? Cuspp { get; set; }

    public DateOnly? FechaAfiliacion { get; set; }
}
