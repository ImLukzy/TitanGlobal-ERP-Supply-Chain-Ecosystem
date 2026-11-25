namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class CertificacionDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public DateOnly FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public bool Vencida { get; set; }
}
