namespace TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;

public sealed record ComponenteListaMaterialDto(
    int ComponenteProductoId,
    string ComponenteProductoNombre,
    decimal Cantidad,
    string UnidadMedida);
