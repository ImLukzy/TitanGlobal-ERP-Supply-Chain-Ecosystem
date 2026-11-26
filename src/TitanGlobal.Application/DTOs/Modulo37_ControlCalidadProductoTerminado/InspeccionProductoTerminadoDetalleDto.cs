namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record InspeccionProductoTerminadoDetalleDto(
    int Id,
    string Numero,
    int OrdenProduccionId,
    string OrdenProduccionNumero,
    int ProductoId,
    string ProductoNombre,
    DateOnly FechaInspeccion,
    int CantidadInspeccionada,
    string Estado,
    decimal? PorcentajeNoConforme,
    decimal ToleranciaNoConformesPorcentaje,
    string Inspector,
    int? CantidadNoConforme,
    int? CantidadConforme,
    DateTime? FechaDictamen,
    string? Observaciones,
    string? MotivoAnulacion,
    bool? SuperaTolerancia);
