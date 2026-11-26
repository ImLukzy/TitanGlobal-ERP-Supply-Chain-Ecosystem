namespace TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

public sealed record OperacionRutaProduccionDto(
    int Secuencia,
    string Nombre,
    int CentroTrabajoId,
    string CentroTrabajoNombre,
    decimal MinutosPreparacion,
    decimal MinutosPorUnidad);
