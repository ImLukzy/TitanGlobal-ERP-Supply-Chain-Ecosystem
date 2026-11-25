namespace TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

public sealed record CrearRutaProduccionRequestDto(
    int ProductoId,
    int Version,
    List<OperacionRutaProduccionRequestDto?>? Operaciones);
