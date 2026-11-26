namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record PaginaInspeccionesProductoTerminadoDto(
    List<InspeccionProductoTerminadoListadoDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
