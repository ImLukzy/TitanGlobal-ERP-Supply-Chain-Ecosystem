namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class CertificacionEmpleadoDto
{
    public int Id { get; set; }

    public int InscripcionId { get; set; }

    public string CursoCodigo { get; set; } = string.Empty;

    public string CursoNombre { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public DateOnly FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public bool Vencida { get; set; }
}
