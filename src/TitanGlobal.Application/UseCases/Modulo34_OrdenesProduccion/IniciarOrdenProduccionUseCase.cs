using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class IniciarOrdenProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public IniciarOrdenProduccionUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        return await _escritura.EjecutarBloqueandoAsync(id, IniciarAsync);

        async Task<int> IniciarAsync()
        {
            OrdenProduccion orden = await ValidacionOrdenProduccion.ObtenerAsync(_unidad, id);
            if (orden.Estado != ValidacionOrdenProduccion.Liberada)
            {
                throw ValidacionOrdenProduccion.Conflicto("Solo se pueden iniciar órdenes de producción liberadas.");
            }

            orden.Estado = ValidacionOrdenProduccion.EnProceso;
            orden.FechaInicioReal = DateTime.UtcNow;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
