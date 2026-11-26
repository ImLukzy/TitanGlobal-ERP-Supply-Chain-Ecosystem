namespace TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

public sealed record RegistrarConsumoMaterialRequestDto(
    int OrdenProduccionId,
    int ComponenteProductoId,
    decimal? Cantidad,
    string? Responsable,
    string? Observaciones);
