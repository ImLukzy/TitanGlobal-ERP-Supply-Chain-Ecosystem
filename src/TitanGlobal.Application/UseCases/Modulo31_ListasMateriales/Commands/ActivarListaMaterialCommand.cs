using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales.Commands;

public class ActivarListaMaterialCommand : IRequest<int>
{
    public int Id { get; set; }
}

public class ActivarListaMaterialCommandHandler : IRequestHandler<ActivarListaMaterialCommand, int>
{
    private readonly IUnitOfWork _unidad;

    private readonly IEscrituraListaMaterial _escritura;

    public ActivarListaMaterialCommandHandler(
        IUnitOfWork unidad,
        IEscrituraListaMaterial escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> Handle(ActivarListaMaterialCommand request, CancellationToken cancellationToken)
    {
        return await _escritura.EjecutarAsync(request.Id, ActivarAsync);

        async Task<int> ActivarAsync()
        {
            ListaMateriales lista = await PersistenciaListaMaterial.ObtenerParaEscrituraAsync(_unidad, request.Id);
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

            return request.Id;
        }
    }
}
