using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class ResumenOrdenesProduccionUseCase
{
    private static readonly string[] EstadosAbiertos =
    {
        ValidacionOrdenProduccion.Planificada,
        ValidacionOrdenProduccion.Liberada,
        ValidacionOrdenProduccion.EnProceso
    };

    private readonly IUnitOfWork _unidad;

    public ResumenOrdenesProduccionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenOrdenesProduccionDto> ExecuteAsync()
    {
        IQueryable<OrdenProduccion> consulta = _unidad.Repository<OrdenProduccion>().AsQueryable();

        // PATRÓN LINQ 9: agrupación por estado con el conteo en la consulta.
        List<EstadoOrdenesProduccionDto> agrupados = await consulta
            .GroupBy(orden => orden.Estado)
            .Select(grupo => new EstadoOrdenesProduccionDto(grupo.Key, grupo.Count()))
            .ToListAsync();

        // PATRÓN LINQ 4: suma asíncrona.
        int abiertas = await consulta
            .Where(orden => EstadosAbiertos.Contains(orden.Estado))
            .SumAsync(orden => orden.CantidadPlanificada);
        int producidas = await consulta
            .Where(orden => orden.Estado == ValidacionOrdenProduccion.Terminada)
            .SumAsync(orden => orden.CantidadProducida ?? 0);

        List<EstadoOrdenesProduccionDto> porEstado = ValidacionOrdenProduccion.Estados
            .Select(estado => new EstadoOrdenesProduccionDto(
                estado,
                agrupados.Where(grupo => grupo.Estado == estado).Sum(grupo => grupo.Total)))
            .ToList();
        return new ResumenOrdenesProduccionDto(
            porEstado.Sum(grupo => grupo.Total), porEstado, abiertas, producidas);
    }
}
