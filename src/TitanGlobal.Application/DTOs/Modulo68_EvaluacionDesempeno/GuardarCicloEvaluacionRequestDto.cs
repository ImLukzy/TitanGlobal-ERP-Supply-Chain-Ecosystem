namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class GuardarCicloEvaluacionRequestDto
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public string? Periodo { get; set; }

    public DateOnly? FechaInicioPeriodo { get; set; }

    public DateOnly? FechaFinPeriodo { get; set; }

    public DateOnly? FechaApertura { get; set; }

    public DateOnly? FechaCierre { get; set; }
}
