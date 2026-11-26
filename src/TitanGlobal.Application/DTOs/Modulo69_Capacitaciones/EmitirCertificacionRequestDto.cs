namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class EmitirCertificacionRequestDto
{
    public string? Codigo { get; set; }

    public DateOnly? FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }
}
