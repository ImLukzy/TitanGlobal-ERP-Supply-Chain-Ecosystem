namespace TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

public sealed record RutaProduccionDetalleDto(
    int Id,
    int ProductoId,
    string ProductoNombre,
    int Version,
    string Estado,
    List<OperacionRutaProduccionDto> Operaciones);
