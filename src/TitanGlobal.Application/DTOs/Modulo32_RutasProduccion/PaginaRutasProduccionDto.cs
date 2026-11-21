namespace TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

public sealed record PaginaRutasProduccionDto(
    List<RutaProduccionListadoDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
