using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class ObtenerInspeccionProductoTerminadoUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerInspeccionProductoTerminadoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<InspeccionProductoTerminadoDetalleDto> ExecuteAsync(int id)
    {
        InspeccionProductoTerminadoDetalleDto? detalle = await _unidad.Repository<InspeccionProductoTerminado>()
            .AsQueryable()
            .Where(inspeccion => inspeccion.Id == id)
            .Select(inspeccion => new InspeccionProductoTerminadoDetalleDto(
                inspeccion.Id,
                inspeccion.Numero,
                inspeccion.OrdenProduccionId,
                inspeccion.OrdenProduccion.Numero,
                inspeccion.OrdenProduccion.ProductoId,
                inspeccion.OrdenProduccion.Producto.Nombre,
                inspeccion.FechaInspeccion,
                inspeccion.CantidadInspeccionada,
                inspeccion.Estado,
                null,
                inspeccion.ToleranciaNoConformesPorcentaje,
                inspeccion.Inspector,
                inspeccion.CantidadNoConforme,
                null,
                inspeccion.FechaDictamen,
                inspeccion.Observaciones,
                inspeccion.MotivoAnulacion,
                null))
            .SingleOrDefaultAsync();
        if (detalle == null)
        {
            throw ValidacionInspeccionProductoTerminado.NoEncontrada(id);
        }

        if (!detalle.CantidadNoConforme.HasValue)
        {
            return detalle;
        }

        int noConforme = detalle.CantidadNoConforme.Value;
        return detalle with
        {
            PorcentajeNoConforme = ValidacionInspeccionProductoTerminado.PorcentajeNoConforme(
                noConforme, detalle.CantidadInspeccionada),
            CantidadConforme = detalle.CantidadInspeccionada - noConforme,
            SuperaTolerancia = ValidacionInspeccionProductoTerminado.SuperaTolerancia(
                noConforme, detalle.CantidadInspeccionada, detalle.ToleranciaNoConformesPorcentaje)
        };
    }
}
