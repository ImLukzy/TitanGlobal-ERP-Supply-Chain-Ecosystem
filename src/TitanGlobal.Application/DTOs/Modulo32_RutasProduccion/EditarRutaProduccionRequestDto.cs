namespace TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

public sealed record EditarRutaProduccionRequestDto(
    List<OperacionRutaProduccionRequestDto?>? Operaciones);
