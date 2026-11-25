namespace TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;

public sealed record InspeccionInsumoDetalleDto(
    int Id,
    string Numero,
    int ProductoId,
    string ProductoNombre,
    int ProveedorId,
    string ProveedorRazonSocial,
    string Lote,
    DateOnly FechaInspeccion,
    decimal CantidadRecibida,
    string UnidadMedida,
    string Estado,
    decimal? PorcentajeDefectos,
    decimal CantidadMuestra,
    decimal ToleranciaDefectosPorcentaje,
    string Inspector,
    decimal? CantidadDefectuosa,
    DateTime? FechaDictamen,
    string? Observaciones,
    string? MotivoAnulacion,
    bool? SuperaTolerancia);
