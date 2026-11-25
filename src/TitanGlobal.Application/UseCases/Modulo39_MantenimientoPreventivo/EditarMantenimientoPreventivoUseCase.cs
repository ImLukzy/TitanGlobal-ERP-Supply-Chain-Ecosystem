using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class EditarMantenimientoPreventivoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraMantenimientoPreventivo _escritura;

    public EditarMantenimientoPreventivoUseCase(IUnitOfWork unidad, IEscrituraMantenimientoPreventivo escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, EditarMantenimientoPreventivoRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, EditarAsync);

        async Task<int> EditarAsync()
        {
            MantenimientoPreventivo mantenimiento = await PersistenciaMantenimientoPreventivo.ObtenerAsync(_unidad, id);
            if (mantenimiento.Estado != ValidacionMantenimientoPreventivo.Programado)
            {
                throw ValidacionMantenimientoPreventivo.Conflicto("Solo se pueden editar mantenimientos programados.");
            }

            string descripcion = ValidacionMantenimientoPreventivo.NormalizarDescripcion(solicitud.Descripcion);
            DateOnly fecha = ValidacionMantenimientoPreventivo.ValidarFechaProgramada(solicitud.FechaProgramada);
            decimal duracion = ValidacionMantenimientoPreventivo.ValidarDuracion(solicitud.DuracionHorasPlanificada);
            int? frecuencia = ValidacionMantenimientoPreventivo.ValidarFrecuencia(solicitud.FrecuenciaDias);
            string? responsable = ValidacionMantenimientoPreventivo.NormalizarResponsable(solicitud.Responsable);
            string codigo = await PersistenciaMantenimientoPreventivo.ObtenerCodigoCentroAsync(
                _unidad, mantenimiento.CentroTrabajoId);
            await PersistenciaMantenimientoPreventivo.ComprobarFechaLibreAsync(
                _unidad, mantenimiento.CentroTrabajoId, codigo, fecha, id);

            mantenimiento.Descripcion = descripcion;
            mantenimiento.FechaProgramada = fecha;
            mantenimiento.DuracionHorasPlanificada = duracion;
            mantenimiento.FrecuenciaDias = frecuencia;
            mantenimiento.Responsable = responsable;
            await PersistenciaMantenimientoPreventivo.GuardarAsync(_unidad, mantenimiento.CentroTrabajoId, codigo, fecha);
            return id;
        }
    }
}
