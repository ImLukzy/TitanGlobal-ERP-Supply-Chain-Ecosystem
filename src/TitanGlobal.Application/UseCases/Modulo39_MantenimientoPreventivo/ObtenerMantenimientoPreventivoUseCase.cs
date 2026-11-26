using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class ObtenerMantenimientoPreventivoUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerMantenimientoPreventivoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<MantenimientoPreventivoDetalleDto> ExecuteAsync(int id)
    {
        DateOnly hoy = ValidacionMantenimientoPreventivo.Hoy();
        MantenimientoPreventivoDetalleDto? detalle = await _unidad.Repository<MantenimientoPreventivo>().AsQueryable()
            .Where(mantenimiento => mantenimiento.Id == id)
            .Select(mantenimiento => new MantenimientoPreventivoDetalleDto(
                mantenimiento.Id,
                mantenimiento.Numero,
                mantenimiento.CentroTrabajoId,
                mantenimiento.CentroTrabajo.Codigo,
                mantenimiento.CentroTrabajo.Nombre,
                mantenimiento.Descripcion,
                mantenimiento.FechaProgramada,
                mantenimiento.DuracionHorasPlanificada,
                mantenimiento.Estado,
                mantenimiento.Estado == ValidacionMantenimientoPreventivo.Programado
                    && mantenimiento.FechaProgramada < hoy,
                mantenimiento.FrecuenciaDias,
                mantenimiento.Responsable,
                mantenimiento.FechaInicioReal,
                mantenimiento.FechaFinReal,
                mantenimiento.HorasReales,
                mantenimiento.Observaciones,
                mantenimiento.MotivoCancelacion,
                null))
            .SingleOrDefaultAsync();
        if (detalle == null)
        {
            throw ValidacionMantenimientoPreventivo.NoEncontrado(id);
        }

        DateOnly? siguiente = ValidacionMantenimientoPreventivo.CalcularSiguiente(
            detalle.Estado, detalle.FechaFinReal, detalle.FrecuenciaDias);
        return detalle with { FechaSiguienteSugerida = siguiente };
    }
}
