namespace TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;

public sealed record GuardarInspeccionInsumoRequestDto(
    int ProductoId,
    int ProveedorId,
    string? Lote,
    DateOnly? FechaInspeccion,
    decimal CantidadRecibida,
    decimal CantidadMuestra,
    string? UnidadMedida,
    decimal ToleranciaDefectosPorcentaje,
    string? Inspector);
