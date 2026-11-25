using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class IniciarMantenimientoPreventivoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraMantenimientoPreventivo _escritura;

    public IniciarMantenimientoPreventivoUseCase(IUnitOfWork unidad, IEscrituraMantenimientoPreventivo escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        return await _escritura.EjecutarAsync(id, IniciarAsync);

        async Task<int> IniciarAsync()
        {
            MantenimientoPreventivo mantenimiento = await PersistenciaMantenimientoPreventivo.ObtenerAsync(_unidad, id);
            if (mantenimiento.Estado != ValidacionMantenimientoPreventivo.Programado)
            {
                throw ValidacionMantenimientoPreventivo.Conflicto("Solo se pueden iniciar mantenimientos programados.");
            }

            mantenimiento.Estado = ValidacionMantenimientoPreventivo.EnEjecucion;
            mantenimiento.FechaInicioReal = DateTime.UtcNow;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
