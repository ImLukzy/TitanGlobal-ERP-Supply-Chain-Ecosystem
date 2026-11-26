namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record EditarOrdenProduccionRequestDto(
    int CantidadPlanificada,
    DateOnly? FechaInicioPlanificada,
    DateOnly? FechaFinPlanificada);
