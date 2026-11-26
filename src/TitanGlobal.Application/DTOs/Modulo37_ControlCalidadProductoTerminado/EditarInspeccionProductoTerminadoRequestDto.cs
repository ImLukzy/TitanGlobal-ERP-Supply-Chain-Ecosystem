namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record EditarInspeccionProductoTerminadoRequestDto(
    DateOnly? FechaInspeccion,
    int CantidadInspeccionada,
    decimal? ToleranciaNoConformesPorcentaje,
    string? Inspector);
