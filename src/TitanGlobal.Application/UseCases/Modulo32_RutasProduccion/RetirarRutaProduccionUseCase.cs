using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

public class RetirarRutaProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraRutaProduccion _escritura;

    public RetirarRutaProduccionUseCase(IUnitOfWork unidad, IEscrituraRutaProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        return await _escritura.EjecutarAsync(id, RetirarAsync);

        async Task<int> RetirarAsync()
        {
            RutaProduccion ruta = await PersistenciaRutaProduccion.ObtenerAsync(_unidad, id);
            if (ruta.Estado != ValidacionRutaProduccion.Activa)
            {
                throw ValidacionRutaProduccion.Conflicto("Solo se pueden retirar rutas de producción activas.");
            }

            ruta.Estado = ValidacionRutaProduccion.Retirada;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
