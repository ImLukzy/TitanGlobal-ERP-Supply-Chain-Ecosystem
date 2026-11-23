namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class CompetenciaEvaluacionDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public decimal Peso { get; set; }
}
