using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

internal static class ConsultaInspeccionInsumo
{
    public static IQueryable<FilaInspeccionInsumo> Proyectar(IQueryable<InspeccionInsumo> consulta)
    {
        // PATRÓN LINQ 3: proyección directa con los nombres del producto y del proveedor.
        return consulta.Select(inspeccion => new FilaInspeccionInsumo(
            inspeccion.Id,
            inspeccion.Numero,
            inspeccion.ProductoId,
            inspeccion.Producto.Nombre,
            inspeccion.ProveedorId,
            inspeccion.Proveedor.RazonSocial,
            inspeccion.Lote,
            inspeccion.FechaInspeccion,
            inspeccion.CantidadRecibida,
            inspeccion.CantidadMuestra,
            inspeccion.UnidadMedida,
            inspeccion.ToleranciaDefectosPorcentaje,
            inspeccion.Inspector,
            inspeccion.Estado,
            inspeccion.CantidadDefectuosa,
            inspeccion.FechaDictamen,
            inspeccion.Observaciones,
            inspeccion.MotivoAnulacion));
    }

    public static InspeccionInsumoListadoDto ALista(FilaInspeccionInsumo fila)
    {
        return new InspeccionInsumoListadoDto(
            fila.Id,
            fila.Numero,
            fila.ProductoId,
            fila.ProductoNombre,
            fila.ProveedorId,
            fila.ProveedorRazonSocial,
            fila.Lote,
            fila.FechaInspeccion,
            fila.CantidadRecibida,
            fila.UnidadMedida,
            fila.Estado,
            ValidacionInspeccionInsumo.Porcentaje(fila.CantidadDefectuosa, fila.CantidadMuestra));
    }

    public static InspeccionInsumoDetalleDto ADetalle(FilaInspeccionInsumo fila)
    {
        decimal? porcentaje = ValidacionInspeccionInsumo.Porcentaje(fila.CantidadDefectuosa, fila.CantidadMuestra);
        return new InspeccionInsumoDetalleDto(
            fila.Id,
            fila.Numero,
            fila.ProductoId,
            fila.ProductoNombre,
            fila.ProveedorId,
            fila.ProveedorRazonSocial,
            fila.Lote,
            fila.FechaInspeccion,
            fila.CantidadRecibida,
            fila.UnidadMedida,
            fila.Estado,
            porcentaje,
            fila.CantidadMuestra,
            fila.ToleranciaDefectosPorcentaje,
            fila.Inspector,
            fila.CantidadDefectuosa,
            fila.FechaDictamen,
            fila.Observaciones,
            fila.MotivoAnulacion,
            ValidacionInspeccionInsumo.SuperaTolerancia(porcentaje, fila.ToleranciaDefectosPorcentaje));
    }
}
