namespace TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;

public sealed record CrearMantenimientoPreventivoRequestDto(
    int CentroTrabajoId,
    string? Descripcion,
    DateOnly? FechaProgramada,
    decimal DuracionHorasPlanificada,
    int? FrecuenciaDias,
    string? Responsable);
