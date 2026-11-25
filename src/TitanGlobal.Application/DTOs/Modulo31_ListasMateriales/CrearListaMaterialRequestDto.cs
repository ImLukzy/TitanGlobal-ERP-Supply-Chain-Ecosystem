namespace TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;

public sealed record CrearListaMaterialRequestDto(
    int ProductoId,
    int Version,
    List<ComponenteListaMaterialRequestDto?>? Componentes);
