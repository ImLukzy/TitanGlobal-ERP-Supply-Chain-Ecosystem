using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class CompletarMantenimientoPreventivoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraMantenimientoPreventivo _escritura;

    public CompletarMantenimientoPreventivoUseCase(IUnitOfWork unidad, IEscrituraMantenimientoPreventivo escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, CompletarMantenimientoPreventivoRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, CompletarAsync);

        async Task<int> CompletarAsync()
        {
            MantenimientoPreventivo mantenimiento = await PersistenciaMantenimientoPreventivo.ObtenerAsync(_unidad, id);
            if (mantenimiento.Estado != ValidacionMantenimientoPreventivo.EnEjecucion)
            {
                throw ValidacionMantenimientoPreventivo.Conflicto("Solo se pueden completar mantenimientos en ejecución.");
            }

            decimal horas = ValidacionMantenimientoPreventivo.ValidarHorasReales(solicitud.HorasReales);
            string? observaciones = ValidacionMantenimientoPreventivo.NormalizarObservaciones(solicitud.Observaciones);
            mantenimiento.Estado = ValidacionMantenimientoPreventivo.Completado;
            mantenimiento.FechaFinReal = DateTime.UtcNow;
            mantenimiento.HorasReales = horas;
            mantenimiento.Observaciones = observaciones;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
