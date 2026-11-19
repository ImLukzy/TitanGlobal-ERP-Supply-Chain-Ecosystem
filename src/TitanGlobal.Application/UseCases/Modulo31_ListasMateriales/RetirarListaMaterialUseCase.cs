using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

public class RetirarListaMaterialUseCase
{
    private readonly IUnitOfWork _unidad;

    private readonly IEscrituraListaMaterial _escritura;

    public RetirarListaMaterialUseCase(IUnitOfWork unidad, IEscrituraListaMaterial escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        return await _escritura.EjecutarAsync(id, RetirarAsync);

        async Task<int> RetirarAsync()
        {
            ListaMateriales lista = await PersistenciaListaMaterial.ObtenerParaEscrituraAsync(_unidad, id);
            if (lista.Estado != ValidacionListaMaterial.Activa)
            {
                throw ValidacionListaMaterial.Conflicto("Solo se pueden retirar listas de materiales activas.");
            }

            lista.Estado = ValidacionListaMaterial.Retirada;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
