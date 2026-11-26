namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class EvaluacionDetalleDto
{
    public int Id { get; set; }

    public int CicloId { get; set; }

    public string CicloCodigo { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public int EvaluadorId { get; set; }

    public string EvaluadorNombre { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Comentario { get; set; }

    public decimal? PuntajeFinal { get; set; }

    public string? Etiqueta { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public DateTime? FechaCierre { get; set; }

    public List<CalificacionEvaluacionDto> Calificaciones { get; set; } = new List<CalificacionEvaluacionDto>();
}
