using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class ObtenerOrdenProduccionUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerOrdenProduccionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<OrdenProduccionDetalleDto> ExecuteAsync(int id)
    {
        OrdenProduccionDetalleDto? cabecera = await _unidad.Repository<OrdenProduccion>().AsQueryable()
            .Where(orden => orden.Id == id)
            .Select(orden => new OrdenProduccionDetalleDto(
                orden.Id,
                orden.Numero,
                orden.ProductoId,
                orden.Producto.Nombre,
                orden.CantidadPlanificada,
                orden.FechaInicioPlanificada,
                orden.FechaFinPlanificada,
                orden.Estado,
                orden.ListaMaterialesId,
                orden.ListaMateriales.Version,
                orden.RutaProduccionId,
                orden.RutaProduccion.Version,
                orden.CantidadProducida,
                orden.FechaInicioReal,
                orden.FechaFinReal,
                orden.MotivoCancelacion,
                0m,
                new List<MaterialRequeridoDto>(),
                new List<OperacionOrdenDto>()))
            .SingleOrDefaultAsync();
        if (cabecera == null)
        {
            throw ValidacionOrdenProduccion.NoEncontrada(id);
        }

        int cantidad = cabecera.CantidadPlanificada;

        // PATRÓN LINQ 10: proyección de las líneas hijas con el cálculo en la consulta.
        List<MaterialRequeridoDto> materiales = await _unidad.Repository<DetalleListaMaterial>().AsQueryable()
            .Where(detalle => detalle.ListaMaterialId == cabecera.ListaMaterialesId)
            .OrderBy(detalle => detalle.ComponenteProductoId)
            .Select(detalle => new MaterialRequeridoDto(
                detalle.ComponenteProductoId,
                detalle.ComponenteProducto.Nombre,
                detalle.Cantidad * cantidad,
                detalle.UnidadMedida))
            .ToListAsync();
        List<OperacionOrdenDto> operaciones = await _unidad.Repository<OperacionRutaProduccion>().AsQueryable()
            .Where(operacion => operacion.RutaProduccionId == cabecera.RutaProduccionId)
            .OrderBy(operacion => operacion.Secuencia)
            .Select(operacion => new OperacionOrdenDto(
                operacion.Secuencia,
                operacion.Nombre,
                operacion.CentroTrabajo.Nombre,
                operacion.MinutosPreparacion + (operacion.MinutosPorUnidad * cantidad)))
            .ToListAsync();

        return cabecera with
        {
            TiempoEstimadoMinutos = operaciones.Sum(operacion => operacion.MinutosEstimados),
            Materiales = materiales,
            Operaciones = operaciones
        };
    }
}
