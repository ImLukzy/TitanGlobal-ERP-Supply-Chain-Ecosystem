namespace TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;

public sealed record PaginaListasMaterialesDto(
    List<ListaMaterialListadoDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
