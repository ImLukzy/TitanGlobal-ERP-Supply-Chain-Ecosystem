namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record ResumenInspeccionesProductoTerminadoDto(
    int TotalInspecciones,
    List<EstadoInspeccionesProductoTerminadoDto> PorEstado,
    decimal TasaRechazoPorcentaje,
    int UnidadesInspeccionadas,
    int UnidadesNoConformes,
    int OrdenesTerminadasSinInspeccion);
