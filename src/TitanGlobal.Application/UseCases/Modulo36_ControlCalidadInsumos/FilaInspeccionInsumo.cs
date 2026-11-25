namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

// Fila leída de la base; el porcentaje se calcula después en C# con el mismo redondeo en InMemory y en PostgreSQL.
internal sealed record FilaInspeccionInsumo(
    int Id,
    string Numero,
    int ProductoId,
    string ProductoNombre,
    int ProveedorId,
    string ProveedorRazonSocial,
    string Lote,
    DateOnly FechaInspeccion,
    decimal CantidadRecibida,
    decimal CantidadMuestra,
    string UnidadMedida,
    decimal ToleranciaDefectosPorcentaje,
    string Inspector,
    string Estado,
    decimal? CantidadDefectuosa,
    DateTime? FechaDictamen,
    string? Observaciones,
    string? MotivoAnulacion);
