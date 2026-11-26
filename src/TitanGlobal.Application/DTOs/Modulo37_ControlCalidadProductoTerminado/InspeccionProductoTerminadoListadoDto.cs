namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record InspeccionProductoTerminadoListadoDto(
    int Id,
    string Numero,
    int OrdenProduccionId,
    string OrdenProduccionNumero,
    int ProductoId,
    string ProductoNombre,
    DateOnly FechaInspeccion,
    int CantidadInspeccionada,
    string Estado,
    decimal? PorcentajeNoConforme);
