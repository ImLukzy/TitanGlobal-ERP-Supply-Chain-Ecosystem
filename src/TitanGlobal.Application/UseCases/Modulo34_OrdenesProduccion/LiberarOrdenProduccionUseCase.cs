using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class LiberarOrdenProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public LiberarOrdenProduccionUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        return await _escritura.EjecutarBloqueandoAsync(id, LiberarAsync);

        async Task<int> LiberarAsync()
        {
            OrdenProduccion orden = await ValidacionOrdenProduccion.ObtenerAsync(_unidad, id);
            if (orden.Estado != ValidacionOrdenProduccion.Planificada)
            {
                throw ValidacionOrdenProduccion.Conflicto("Solo se pueden liberar órdenes de producción planificadas.");
            }

            orden.Estado = ValidacionOrdenProduccion.Liberada;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
