namespace TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

public sealed record ConsumoMaterialDetalleDto(
    int Id,
    string Numero,
    int OrdenProduccionId,
    string OrdenProduccionNumero,
    int ComponenteProductoId,
    string ComponenteProductoNombre,
    decimal Cantidad,
    string UnidadMedida,
    DateTime FechaRegistro,
    string Estado,
    string ProductoFabricadoNombre,
    string? Responsable,
    string? Observaciones,
    DateTime? FechaAnulacion,
    string? MotivoAnulacion);
