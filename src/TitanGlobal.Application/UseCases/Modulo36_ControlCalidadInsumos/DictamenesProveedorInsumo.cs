namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

// Conteos de dictámenes de un proveedor, agregados en la consulta del resumen.
internal sealed record DictamenesProveedorInsumo(int ProveedorId, string RazonSocial, int Aprobadas, int Rechazadas);
