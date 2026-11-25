namespace TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;

public sealed record CompletarMantenimientoPreventivoRequestDto(
    decimal HorasReales,
    string? Observaciones);
