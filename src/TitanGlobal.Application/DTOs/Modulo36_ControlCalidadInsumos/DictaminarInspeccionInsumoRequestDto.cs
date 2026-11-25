namespace TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;

public sealed record DictaminarInspeccionInsumoRequestDto(
    string? Resultado,
    decimal CantidadDefectuosa,
    string? Observaciones);
