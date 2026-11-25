namespace TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;

public class EvaluacionDetalleDto
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public int PuntajeCumplimiento { get; set; }

    public int PuntajeCalidad { get; set; }

    public int PuntajePlazos { get; set; }

    public decimal PuntajeFinal { get; set; }

    public string Calificacion { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }
}
