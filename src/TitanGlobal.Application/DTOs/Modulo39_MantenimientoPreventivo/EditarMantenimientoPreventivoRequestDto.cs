namespace TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;

public sealed record EditarMantenimientoPreventivoRequestDto(
    string? Descripcion,
    DateOnly? FechaProgramada,
    decimal DuracionHorasPlanificada,
    int? FrecuenciaDias,
    string? Responsable);
