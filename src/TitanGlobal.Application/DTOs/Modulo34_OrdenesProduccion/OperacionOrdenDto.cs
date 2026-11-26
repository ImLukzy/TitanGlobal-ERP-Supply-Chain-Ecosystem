namespace TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;

public sealed record OperacionOrdenDto(
    int Secuencia,
    string Nombre,
    string CentroTrabajoNombre,
    decimal MinutosEstimados);
