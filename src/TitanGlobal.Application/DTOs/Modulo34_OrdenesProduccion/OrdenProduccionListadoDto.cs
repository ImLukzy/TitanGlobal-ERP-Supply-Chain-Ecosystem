namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record OrdenProduccionListadoDto(
    int Id,
    string Numero,
    int ProductoId,
    string ProductoNombre,
    int CantidadPlanificada,
    DateOnly FechaInicioPlanificada,
    DateOnly FechaFinPlanificada,
    string Estado);
