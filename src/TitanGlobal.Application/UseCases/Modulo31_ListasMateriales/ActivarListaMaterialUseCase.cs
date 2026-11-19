using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

public class ActivarListaMaterialUseCase
{
    private readonly IUnitOfWork _unidad;

    private readonly IEscrituraListaMaterial _escritura;

    public ActivarListaMaterialUseCase(IUnitOfWork unidad, IEscrituraListaMaterial escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        return await _escritura.EjecutarAsync(id, ActivarAsync);

        async Task<int> ActivarAsync()
        {
            ListaMateriales lista = await PersistenciaListaMaterial.ObtenerParaEscrituraAsync(_unidad, id);
            if (lista.Estado != ValidacionListaMaterial.Borrador)
            {
                throw ValidacionListaMaterial.Conflicto("Solo se pueden activar listas de materiales en borrador.");
            }

            string mensajeActiva = $"El producto {lista.ProductoId} ya tiene una lista de materiales activa.";
            bool existeActiva = await _unidad.Repository<ListaMateriales>().AsQueryable()
                .AnyAsync(otra => otra.ProductoId == lista.ProductoId && otra.Estado == ValidacionListaMaterial.Activa);
            if (existeActiva)
            {
                throw ValidacionListaMaterial.Conflicto(mensajeActiva);
            }

            lista.Estado = ValidacionListaMaterial.Activa;
            try
            {
                await _unidad.CompleteAsync();
            }
            catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad
                && excepcion.Restriccion == PersistenciaListaMaterial.IndiceActiva)
            {
                throw ValidacionListaMaterial.Conflicto(mensajeActiva);
            }

            return id;
        }
    }
}
