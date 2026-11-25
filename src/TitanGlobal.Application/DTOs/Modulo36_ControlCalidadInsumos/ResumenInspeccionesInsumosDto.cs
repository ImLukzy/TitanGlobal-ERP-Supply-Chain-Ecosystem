namespace TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;

public sealed record ResumenInspeccionesInsumosDto(
    int TotalInspecciones,
    List<EstadoInspeccionesInsumosDto> PorEstado,
    decimal TasaRechazoPorcentaje,
    List<ProveedorInspeccionesInsumosDto> PorProveedor);
