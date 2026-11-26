namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record CrearOrdenProduccionRequestDto(
    int ProductoId,
    int CantidadPlanificada,
    DateOnly? FechaInicioPlanificada,
    DateOnly? FechaFinPlanificada);
