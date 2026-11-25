using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

public class AnularInspeccionInsumoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraInspeccionInsumo _escritura;

    public AnularInspeccionInsumoUseCase(IUnitOfWork unidad, IEscrituraInspeccionInsumo escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, AnularInspeccionInsumoRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, AnularAsync);

        async Task<int> AnularAsync()
        {
            InspeccionInsumo inspeccion = await PersistenciaInspeccionInsumo.ObtenerAsync(_unidad, id);
            if (inspeccion.Estado != ValidacionInspeccionInsumo.Pendiente)
            {
                throw ValidacionInspeccionInsumo.Conflicto("Solo se pueden anular inspecciones pendientes.");
            }

            inspeccion.MotivoAnulacion = ValidacionInspeccionInsumo.NormalizarMotivo(solicitud.Motivo);
            inspeccion.Estado = ValidacionInspeccionInsumo.Anulada;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
