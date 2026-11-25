namespace TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;

public sealed record RutaProduccionListadoDto(
    int Id,
    int ProductoId,
    string ProductoNombre,
    int Version,
    string Estado,
    int TotalOperaciones);
