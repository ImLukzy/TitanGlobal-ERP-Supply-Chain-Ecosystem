namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class CicloEvaluacionListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public DateOnly FechaInicioPeriodo { get; set; }

    public DateOnly FechaFinPeriodo { get; set; }

    public DateOnly FechaApertura { get; set; }

    public DateOnly FechaCierre { get; set; }

    public string Estado { get; set; } = string.Empty;
}
