namespace TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

public sealed record LineaBalanceConsumoDto(
    int ComponenteProductoId,
    string ComponenteProductoNombre,
    string UnidadMedida,
    decimal CantidadRequerida,
    decimal CantidadConsumida,
    decimal CantidadPendiente,
    decimal DesviacionPorcentaje);
