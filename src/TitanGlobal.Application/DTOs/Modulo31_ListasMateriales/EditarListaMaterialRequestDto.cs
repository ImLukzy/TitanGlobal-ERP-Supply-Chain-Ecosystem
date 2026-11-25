namespace TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;

public sealed record EditarListaMaterialRequestDto(
    List<ComponenteListaMaterialRequestDto?>? Componentes);
