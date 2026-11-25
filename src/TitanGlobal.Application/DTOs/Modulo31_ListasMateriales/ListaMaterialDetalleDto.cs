namespace TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;

public sealed record ListaMaterialDetalleDto(
    int Id,
    int ProductoId,
    string ProductoNombre,
    int Version,
    string Estado,
    List<ComponenteListaMaterialDto> Componentes);
