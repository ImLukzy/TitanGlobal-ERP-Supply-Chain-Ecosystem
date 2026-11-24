namespace TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;

public class ActualizarEvaluacionRequestDto
{
    public int PuntajeCumplimiento { get; set; }

    public int PuntajeCalidad { get; set; }

    public int PuntajePlazos { get; set; }

    public string? Observaciones { get; set; }
}
