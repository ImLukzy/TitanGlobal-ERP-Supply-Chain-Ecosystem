namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record CrearInspeccionProductoTerminadoRequestDto(
    int OrdenProduccionId,
    DateOnly? FechaInspeccion,
    int CantidadInspeccionada,
    decimal? ToleranciaNoConformesPorcentaje,
    string? Inspector);
