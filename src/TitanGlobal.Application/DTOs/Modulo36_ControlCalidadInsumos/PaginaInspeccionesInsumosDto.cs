namespace TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;

public sealed record PaginaInspeccionesInsumosDto(
    List<InspeccionInsumoListadoDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
