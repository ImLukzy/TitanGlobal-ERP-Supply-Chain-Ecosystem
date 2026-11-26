namespace TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

public sealed record BalanceConsumoOrdenDto(
    int OrdenProduccionId,
    string OrdenProduccionNumero,
    string ProductoNombre,
    int CantidadPlanificada,
    string EstadoOrden,
    List<LineaBalanceConsumoDto> Lineas);
