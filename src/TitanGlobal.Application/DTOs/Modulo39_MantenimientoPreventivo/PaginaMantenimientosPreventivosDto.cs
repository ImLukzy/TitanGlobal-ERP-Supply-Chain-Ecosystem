namespace TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;

public sealed record PaginaMantenimientosPreventivosDto(
    List<MantenimientoPreventivoListadoDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
