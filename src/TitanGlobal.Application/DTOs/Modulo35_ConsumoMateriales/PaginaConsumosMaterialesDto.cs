namespace TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

public sealed record PaginaConsumosMaterialesDto(
    List<ConsumoMaterialListadoDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
