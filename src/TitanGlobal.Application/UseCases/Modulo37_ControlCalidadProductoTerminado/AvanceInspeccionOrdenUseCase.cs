using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class AvanceInspeccionOrdenUseCase
{
    private readonly IUnitOfWork _unidad;

    public AvanceInspeccionOrdenUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AvanceInspeccionOrdenDto> ExecuteAsync(int ordenProduccionId)
    {
        OrdenProduccion? orden = await _unidad.Repository<OrdenProduccion>().AsQueryable()
            .Include(existente => existente.Producto)
            .SingleOrDefaultAsync(existente => existente.Id == ordenProduccionId);
        if (orden == null)
        {
            throw ValidacionInspeccionProductoTerminado.OrdenNoEncontrada(ordenProduccionId);
        }

        if (orden.Estado != ValidacionInspeccionProductoTerminado.OrdenTerminada)
        {
            return new AvanceInspeccionOrdenDto(
                orden.Id, orden.Numero, orden.Producto.Nombre, orden.Estado, null, 0, 0, 0, 0);
        }

        IQueryable<InspeccionProductoTerminado> vigentes = _unidad.Repository<InspeccionProductoTerminado>()
            .AsQueryable()
            .Where(inspeccion => inspeccion.OrdenProduccionId == ordenProduccionId
                && inspeccion.Estado != ValidacionInspeccionProductoTerminado.Anulada);

        // PATRÓN LINQ 4: sumas asíncronas sobre las inspecciones no anuladas de la orden.
        int inspeccionadas = await vigentes.SumAsync(inspeccion => inspeccion.CantidadInspeccionada);
        int conformesAprobadas = await vigentes
            .Where(inspeccion => inspeccion.Estado == ValidacionInspeccionProductoTerminado.Aprobada)
            .SumAsync(inspeccion => inspeccion.CantidadInspeccionada - (inspeccion.CantidadNoConforme ?? 0));
        int noConformes = await vigentes
            .Where(inspeccion => inspeccion.Estado == ValidacionInspeccionProductoTerminado.Aprobada
                || inspeccion.Estado == ValidacionInspeccionProductoTerminado.Rechazada)
            .SumAsync(inspeccion => inspeccion.CantidadNoConforme ?? 0);

        int producida = orden.CantidadProducida ?? 0;
        return new AvanceInspeccionOrdenDto(
            orden.Id,
            orden.Numero,
            orden.Producto.Nombre,
            orden.Estado,
            orden.CantidadProducida,
            inspeccionadas,
            Math.Max(producida - inspeccionadas, 0),
            conformesAprobadas,
            noConformes);
    }
}
