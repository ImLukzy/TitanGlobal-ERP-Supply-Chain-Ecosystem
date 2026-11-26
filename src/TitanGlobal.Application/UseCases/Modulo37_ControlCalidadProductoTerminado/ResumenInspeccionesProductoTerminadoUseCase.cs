using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class ResumenInspeccionesProductoTerminadoUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenInspeccionesProductoTerminadoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenInspeccionesProductoTerminadoDto> ExecuteAsync()
    {
        IQueryable<InspeccionProductoTerminado> consulta =
            _unidad.Repository<InspeccionProductoTerminado>().AsQueryable();
        IQueryable<InspeccionProductoTerminado> dictaminadas = consulta
            .Where(inspeccion => inspeccion.Estado == ValidacionInspeccionProductoTerminado.Aprobada
                || inspeccion.Estado == ValidacionInspeccionProductoTerminado.Rechazada);

        // PATRÓN LINQ 9: agrupación por estado con el conteo en la consulta.
        List<EstadoInspeccionesProductoTerminadoDto> agrupados = await consulta
            .GroupBy(inspeccion => inspeccion.Estado)
            .Select(grupo => new EstadoInspeccionesProductoTerminadoDto(grupo.Key, grupo.Count()))
            .ToListAsync();

        // PATRÓN LINQ 4: sumas asíncronas de las inspecciones dictaminadas.
        int unidadesInspeccionadas = await dictaminadas.SumAsync(inspeccion => inspeccion.CantidadInspeccionada);
        int unidadesNoConformes = await dictaminadas.SumAsync(inspeccion => inspeccion.CantidadNoConforme ?? 0);

        // PATRÓN LINQ 12: navegación inversa para hallar órdenes terminadas sin inspecciones vigentes.
        int sinInspeccion = await _unidad.Repository<OrdenProduccion>().AsQueryable()
            .CountAsync(orden => orden.Estado == ValidacionInspeccionProductoTerminado.OrdenTerminada
                && !orden.InspeccionesProductoTerminado.Any(
                    inspeccion => inspeccion.Estado != ValidacionInspeccionProductoTerminado.Anulada));

        // porEstado sigue el orden del ciclo y solo incluye estados con filas.
        List<EstadoInspeccionesProductoTerminadoDto> porEstado = ValidacionInspeccionProductoTerminado.Estados
            .Select(estado => agrupados.FirstOrDefault(grupo => grupo.Estado == estado))
            .OfType<EstadoInspeccionesProductoTerminadoDto>()
            .ToList();
        int aprobadas = Total(porEstado, ValidacionInspeccionProductoTerminado.Aprobada);
        int rechazadas = Total(porEstado, ValidacionInspeccionProductoTerminado.Rechazada);
        decimal tasa = aprobadas + rechazadas == 0
            ? 0m
            : ValidacionInspeccionProductoTerminado.Redondear(rechazadas * 100m / (aprobadas + rechazadas));

        return new ResumenInspeccionesProductoTerminadoDto(
            porEstado.Sum(grupo => grupo.Total),
            porEstado,
            tasa,
            unidadesInspeccionadas,
            unidadesNoConformes,
            sinInspeccion);
    }

    private static int Total(List<EstadoInspeccionesProductoTerminadoDto> porEstado, string estado)
    {
        return porEstado.Where(grupo => grupo.Estado == estado).Sum(grupo => grupo.Total);
    }
}
