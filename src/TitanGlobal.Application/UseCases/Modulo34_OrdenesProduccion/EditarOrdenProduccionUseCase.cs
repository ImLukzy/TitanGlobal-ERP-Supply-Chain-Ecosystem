using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class EditarOrdenProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public EditarOrdenProduccionUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, EditarOrdenProduccionRequestDto solicitud)
    {
        return await _escritura.EjecutarBloqueandoAsync(id, EditarAsync);

        async Task<int> EditarAsync()
        {
            OrdenProduccion orden = await ValidacionOrdenProduccion.ObtenerAsync(_unidad, id);
            if (orden.Estado != ValidacionOrdenProduccion.Planificada)
            {
                throw ValidacionOrdenProduccion.Conflicto("Solo se pueden editar órdenes de producción planificadas.");
            }

            int cantidad = ValidacionOrdenProduccion.ValidarCantidadPlanificada(solicitud.CantidadPlanificada);
            (DateOnly inicio, DateOnly fin) = ValidacionOrdenProduccion.ValidarFechasPlanificadas(
                solicitud.FechaInicioPlanificada, solicitud.FechaFinPlanificada);
            orden.CantidadPlanificada = cantidad;
            orden.FechaInicioPlanificada = inicio;
            orden.FechaFinPlanificada = fin;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
