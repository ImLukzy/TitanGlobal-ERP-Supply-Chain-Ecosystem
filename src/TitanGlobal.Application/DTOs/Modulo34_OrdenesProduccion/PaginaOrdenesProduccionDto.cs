namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record PaginaOrdenesProduccionDto(
    List<OrdenProduccionListadoDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
