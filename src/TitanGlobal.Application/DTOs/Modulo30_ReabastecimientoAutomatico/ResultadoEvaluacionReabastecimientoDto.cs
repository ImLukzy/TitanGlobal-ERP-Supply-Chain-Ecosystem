namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class ResultadoEvaluacionReabastecimientoDto
{
    public DateTime FechaEvaluacion { get; set; }

    public int ReglasEvaluadas { get; set; }

    public int ReglasBajoPuntoReorden { get; set; }

    public int SugerenciasGeneradas { get; set; }

    public int SugerenciasActualizadas { get; set; }

    public List<EjecucionReabastecimientoItemDto> Items { get; set; } = new List<EjecucionReabastecimientoItemDto>();
}
