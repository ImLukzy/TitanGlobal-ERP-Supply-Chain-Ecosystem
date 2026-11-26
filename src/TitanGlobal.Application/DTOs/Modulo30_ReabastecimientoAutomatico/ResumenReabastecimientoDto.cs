namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class ResumenReabastecimientoDto
{
    public int TotalReglas { get; set; }

    public int ReglasActivas { get; set; }

    public int ReglasEnAlerta { get; set; }

    public int SugerenciasPendientes { get; set; }

    public int SolicitudesGeneradas { get; set; }

    public int UnidadesSugeridasPendientes { get; set; }
}
