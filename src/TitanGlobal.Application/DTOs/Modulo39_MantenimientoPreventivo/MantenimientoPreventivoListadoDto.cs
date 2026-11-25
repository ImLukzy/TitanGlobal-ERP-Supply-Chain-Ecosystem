namespace TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;

public sealed record MantenimientoPreventivoListadoDto(
    int Id,
    string Numero,
    int CentroTrabajoId,
    string CentroTrabajoCodigo,
    string CentroTrabajoNombre,
    string Descripcion,
    DateOnly FechaProgramada,
    decimal DuracionHorasPlanificada,
    string Estado,
    bool Vencido);
