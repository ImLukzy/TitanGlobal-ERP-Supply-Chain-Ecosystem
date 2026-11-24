namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class ReglaReabastecimientoDetalleDto : ReglaReabastecimientoListadoDto
{
    public string? Observaciones { get; set; }

    public List<EjecucionReabastecimientoItemDto> EjecucionesRecientes { get; set; } = new List<EjecucionReabastecimientoItemDto>();
}
