namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record OrdenProduccionDetalleDto(
    int Id,
    string Numero,
    int ProductoId,
    string ProductoNombre,
    int CantidadPlanificada,
    DateOnly FechaInicioPlanificada,
    DateOnly FechaFinPlanificada,
    string Estado,
    int ListaMaterialesId,
    int ListaMaterialesVersion,
    int RutaProduccionId,
    int RutaProduccionVersion,
    int? CantidadProducida,
    DateTime? FechaInicioReal,
    DateTime? FechaFinReal,
    string? MotivoCancelacion,
    decimal TiempoEstimadoMinutos,
    List<MaterialRequeridoDto> Materiales,
    List<OperacionOrdenDto> Operaciones);
