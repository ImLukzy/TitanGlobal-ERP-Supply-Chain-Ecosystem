namespace TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

public sealed record ConsumoMaterialListadoDto(
    int Id,
    string Numero,
    int OrdenProduccionId,
    string OrdenProduccionNumero,
    int ComponenteProductoId,
    string ComponenteProductoNombre,
    decimal Cantidad,
    string UnidadMedida,
    DateTime FechaRegistro,
    string Estado);
