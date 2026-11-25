namespace TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;

public sealed record ListaMaterialListadoDto(
    int Id,
    int ProductoId,
    string ProductoNombre,
    int Version,
    string Estado,
    int TotalComponentes);
