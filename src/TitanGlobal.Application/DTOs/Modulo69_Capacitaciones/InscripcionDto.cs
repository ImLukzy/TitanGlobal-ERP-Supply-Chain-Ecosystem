namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class InscripcionDto
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal? Nota { get; set; }

    public DateOnly FechaInscripcion { get; set; }

    public CertificacionDto? Certificacion { get; set; }
}
