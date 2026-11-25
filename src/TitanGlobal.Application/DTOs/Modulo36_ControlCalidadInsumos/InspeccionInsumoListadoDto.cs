namespace TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;

public sealed record InspeccionInsumoListadoDto(
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
    decimal? PorcentajeDefectos);
