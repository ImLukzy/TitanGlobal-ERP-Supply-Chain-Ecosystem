namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class GuardarCalificacionesRequestDto
{
    public List<CalificacionRequestDto>? Calificaciones { get; set; }

    public string? Comentario { get; set; }
}
