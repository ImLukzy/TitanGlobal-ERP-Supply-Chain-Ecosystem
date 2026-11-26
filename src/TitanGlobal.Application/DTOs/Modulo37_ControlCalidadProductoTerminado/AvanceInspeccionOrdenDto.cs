namespace TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;

public sealed record AvanceInspeccionOrdenDto(
    int OrdenProduccionId,
    string OrdenProduccionNumero,
    string ProductoNombre,
    string EstadoOrden,
    int? CantidadProducida,
    int UnidadesInspeccionadas,
    int UnidadesPorInspeccionar,
    int UnidadesConformesAprobadas,
    int UnidadesNoConformes);
