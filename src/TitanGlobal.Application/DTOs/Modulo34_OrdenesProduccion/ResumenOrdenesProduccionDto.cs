namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record ResumenOrdenesProduccionDto(
    int TotalOrdenes,
    List<EstadoOrdenesProduccionDto> PorEstado,
    int UnidadesPlanificadasAbiertas,
    int UnidadesProducidas);
