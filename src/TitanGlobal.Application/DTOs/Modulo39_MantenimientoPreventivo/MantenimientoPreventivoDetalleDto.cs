namespace TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;

public sealed record MantenimientoPreventivoDetalleDto(
    int Id,
    string Numero,
    int CentroTrabajoId,
    string CentroTrabajoCodigo,
    string CentroTrabajoNombre,
    string Descripcion,
    DateOnly FechaProgramada,
    decimal DuracionHorasPlanificada,
    string Estado,
    bool Vencido,
    int? FrecuenciaDias,
    string? Responsable,
    DateTime? FechaInicioReal,
    DateTime? FechaFinReal,
    decimal? HorasReales,
    string? Observaciones,
    string? MotivoCancelacion,
    DateOnly? FechaSiguienteSugerida);
