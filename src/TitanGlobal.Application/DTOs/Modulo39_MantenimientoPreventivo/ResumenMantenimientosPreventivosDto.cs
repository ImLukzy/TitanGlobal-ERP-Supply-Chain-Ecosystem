namespace TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;

public sealed record ResumenMantenimientosPreventivosDto(
    int TotalMantenimientos,
    List<EstadoMantenimientosPreventivosDto> PorEstado,
    int Vencidos,
    int Proximos7Dias,
    decimal HorasPlanificadasPendientes);
