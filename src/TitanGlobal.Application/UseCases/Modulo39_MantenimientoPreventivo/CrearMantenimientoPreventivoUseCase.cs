using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class CrearMantenimientoPreventivoUseCase
{
    private readonly IUnitOfWork _unidad;

    public CrearMantenimientoPreventivoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<MantenimientoPreventivoCreadoDto> ExecuteAsync(CrearMantenimientoPreventivoRequestDto solicitud)
    {
        ValidacionMantenimientoPreventivo.ValidarIdCentro(solicitud.CentroTrabajoId);
        string descripcion = ValidacionMantenimientoPreventivo.NormalizarDescripcion(solicitud.Descripcion);
        DateOnly fecha = ValidacionMantenimientoPreventivo.ValidarFechaProgramada(solicitud.FechaProgramada);
        decimal duracion = ValidacionMantenimientoPreventivo.ValidarDuracion(solicitud.DuracionHorasPlanificada);
        int? frecuencia = ValidacionMantenimientoPreventivo.ValidarFrecuencia(solicitud.FrecuenciaDias);
        string? responsable = ValidacionMantenimientoPreventivo.NormalizarResponsable(solicitud.Responsable);

        CentroTrabajo? centro = await _unidad.Repository<CentroTrabajo>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Id == solicitud.CentroTrabajoId);
        if (centro == null)
        {
            throw ValidacionMantenimientoPreventivo.CentroInexistente(solicitud.CentroTrabajoId);
        }

        if (!centro.Activo)
        {
            throw ValidacionMantenimientoPreventivo.CentroInactivo(solicitud.CentroTrabajoId);
        }

        await PersistenciaMantenimientoPreventivo.ComprobarFechaLibreAsync(
            _unidad, solicitud.CentroTrabajoId, centro.Codigo, fecha, null);

        MantenimientoPreventivo mantenimiento = new MantenimientoPreventivo
        {
            Numero = ValidacionMantenimientoPreventivo.NuevoNumeroTemporal(),
            CentroTrabajoId = solicitud.CentroTrabajoId,
            Descripcion = descripcion,
            FechaProgramada = fecha,
            DuracionHorasPlanificada = duracion,
            FrecuenciaDias = frecuencia,
            Responsable = responsable,
            Estado = ValidacionMantenimientoPreventivo.Programado
        };

        // El número definitivo sale del Id: las dos escrituras van en la misma transacción.
        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await _unidad.Repository<MantenimientoPreventivo>().AddAsync(mantenimiento);
            await PersistenciaMantenimientoPreventivo.GuardarAsync(
                _unidad, solicitud.CentroTrabajoId, centro.Codigo, fecha);
            mantenimiento.Numero = ValidacionMantenimientoPreventivo.NumeroDefinitivo(mantenimiento.Id);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }

        return new MantenimientoPreventivoCreadoDto(mantenimiento.Id, mantenimiento.Numero);
    }
}
