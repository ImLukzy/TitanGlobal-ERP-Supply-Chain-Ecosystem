namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class CalificacionEvaluacionDto
{
    public int CompetenciaId { get; set; }

    public string Competencia { get; set; } = string.Empty;

    public decimal Peso { get; set; }

    public int? Calificacion { get; set; }
}
