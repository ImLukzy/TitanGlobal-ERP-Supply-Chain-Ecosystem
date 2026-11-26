namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record DictaminarInspeccionProductoTerminadoRequestDto(
    string? Resultado,
    int? CantidadNoConforme,
    string? Observaciones);
