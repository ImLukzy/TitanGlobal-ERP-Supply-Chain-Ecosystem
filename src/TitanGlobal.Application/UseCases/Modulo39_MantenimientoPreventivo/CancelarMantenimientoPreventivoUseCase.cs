using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class CancelarMantenimientoPreventivoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraMantenimientoPreventivo _escritura;

    public CancelarMantenimientoPreventivoUseCase(IUnitOfWork unidad, IEscrituraMantenimientoPreventivo escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, CancelarMantenimientoPreventivoRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, CancelarAsync);

        async Task<int> CancelarAsync()
        {
            MantenimientoPreventivo mantenimiento = await PersistenciaMantenimientoPreventivo.ObtenerAsync(_unidad, id);
            if (mantenimiento.Estado != ValidacionMantenimientoPreventivo.Programado)
            {
                throw ValidacionMantenimientoPreventivo.Conflicto("Solo se pueden cancelar mantenimientos programados.");
            }

            mantenimiento.MotivoCancelacion = ValidacionMantenimientoPreventivo.NormalizarMotivo(solicitud.Motivo);
            mantenimiento.Estado = ValidacionMantenimientoPreventivo.Cancelado;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
