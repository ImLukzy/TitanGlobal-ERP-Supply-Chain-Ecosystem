using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

public class EditarListaMaterialUseCase
{
    private readonly IUnitOfWork _unidad;

    private readonly IEscrituraListaMaterial _escritura;

    public EditarListaMaterialUseCase(IUnitOfWork unidad, IEscrituraListaMaterial escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, EditarListaMaterialRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, EditarAsync);

        async Task<int> EditarAsync()
        {
            ListaMateriales lista = await PersistenciaListaMaterial.ObtenerParaEscrituraAsync(_unidad, id);
            if (lista.Estado != ValidacionListaMaterial.Borrador)
            {
                throw ValidacionListaMaterial.Conflicto("Solo se pueden editar listas de materiales en borrador.");
            }

            List<DetalleListaMaterial> detalles = await ValidacionListaMaterial.ValidarComponentesAsync(
                _unidad, lista.ProductoId, solicitud.Componentes);
            await ReemplazarComponentesAsync(id, detalles);
            await PersistenciaListaMaterial.GuardarComponentesAsync(_unidad, lista.ProductoId, detalles);
            return id;
        }
    }

    private async Task ReemplazarComponentesAsync(int id, List<DetalleListaMaterial> detalles)
    {
        IGenericRepository<DetalleListaMaterial> repositorio = _unidad.Repository<DetalleListaMaterial>();
        List<DetalleListaMaterial> actuales = await repositorio.AsQueryable()
            .Where(detalle => detalle.ListaMaterialId == id)
            .ToListAsync();
        Dictionary<int, DetalleListaMaterial> pendientes =
            detalles.ToDictionary(detalle => detalle.ComponenteProductoId);

        foreach (DetalleListaMaterial actual in actuales)
        {
            if (pendientes.Remove(actual.ComponenteProductoId, out DetalleListaMaterial? nuevo))
            {
                actual.Cantidad = nuevo.Cantidad;
                actual.UnidadMedida = nuevo.UnidadMedida;
            }
            else
            {
                repositorio.Remove(actual);
            }
        }

        foreach (DetalleListaMaterial nuevo in pendientes.Values)
        {
            nuevo.ListaMaterialId = id;
            await repositorio.AddAsync(nuevo);
        }
    }
}
