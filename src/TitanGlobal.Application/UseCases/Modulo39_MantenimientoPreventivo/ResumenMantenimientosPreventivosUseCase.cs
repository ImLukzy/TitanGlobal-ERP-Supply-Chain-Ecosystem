using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class ResumenMantenimientosPreventivosUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenMantenimientosPreventivosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenMantenimientosPreventivosDto> ExecuteAsync()
    {
        IQueryable<MantenimientoPreventivo> mantenimientos = _unidad.Repository<MantenimientoPreventivo>().AsQueryable();
        DateOnly hoy = ValidacionMantenimientoPreventivo.Hoy();
        DateOnly limite = hoy.AddDays(6);
        int total = await mantenimientos.CountAsync();
        // PATRÓN LINQ 9: agrupación con orden explícito.
        List<EstadoMantenimientosPreventivosDto> porEstado = await mantenimientos
            .GroupBy(mantenimiento => mantenimiento.Estado)
            .OrderBy(grupo => grupo.Key == ValidacionMantenimientoPreventivo.Programado ? 0
                : grupo.Key == ValidacionMantenimientoPreventivo.EnEjecucion ? 1
                : grupo.Key == ValidacionMantenimientoPreventivo.Completado ? 2 : 3)
            .Select(grupo => new EstadoMantenimientosPreventivosDto(grupo.Key, grupo.Count()))
            .ToListAsync();
        IQueryable<MantenimientoPreventivo> programados = mantenimientos
            .Where(mantenimiento => mantenimiento.Estado == ValidacionMantenimientoPreventivo.Programado);
        int vencidos = await programados.CountAsync(mantenimiento => mantenimiento.FechaProgramada < hoy);
        // PATRÓN LINQ 6: filtro por rango de fecha.
        int proximos = await programados.CountAsync(mantenimiento =>
            mantenimiento.FechaProgramada >= hoy && mantenimiento.FechaProgramada <= limite);
        // PATRÓN LINQ 4: suma asíncrona.
        decimal horasPendientes = await mantenimientos
            .Where(mantenimiento => mantenimiento.Estado == ValidacionMantenimientoPreventivo.Programado
                || mantenimiento.Estado == ValidacionMantenimientoPreventivo.EnEjecucion)
            .SumAsync(mantenimiento => mantenimiento.DuracionHorasPlanificada);
        return new ResumenMantenimientosPreventivosDto(total, porEstado, vencidos, proximos, horasPendientes);
    }
}
