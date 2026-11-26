namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record MaterialRequeridoDto(
    int ComponenteProductoId,
    string ComponenteProductoNombre,
    decimal CantidadRequerida,
    string UnidadMedida);
