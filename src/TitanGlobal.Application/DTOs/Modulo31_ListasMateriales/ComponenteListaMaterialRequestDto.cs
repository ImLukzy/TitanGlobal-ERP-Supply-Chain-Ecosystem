namespace TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;

public sealed record ComponenteListaMaterialRequestDto(
    int ComponenteProductoId,
    decimal Cantidad,
    string? UnidadMedida);
