namespace TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

public sealed record OperacionRutaProduccionRequestDto(
    int Secuencia,
    string? Nombre,
    int CentroTrabajoId,
    decimal? MinutosPreparacion,
    decimal? MinutosPorUnidad);
