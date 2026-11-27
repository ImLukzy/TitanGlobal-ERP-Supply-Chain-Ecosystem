using MediatR;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales.Commands;

public class RetirarListaMaterialCommand : IRequest<int>
{
    public int Id { get; set; }
}

public class RetirarListaMaterialCommandHandler : IRequestHandler<RetirarListaMaterialCommand, int>
{
    private readonly IUnitOfWork _unidad;

    private readonly IEscrituraListaMaterial _escritura;

    public RetirarListaMaterialCommandHandler(
        IUnitOfWork unidad,
        IEscrituraListaMaterial escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> Handle(RetirarListaMaterialCommand request, CancellationToken cancellationToken)
    {
        return await _escritura.EjecutarAsync(request.Id, RetirarAsync);

        async Task<int> RetirarAsync()
        {
            ListaMateriales lista = await PersistenciaListaMaterial.ObtenerParaEscrituraAsync(_unidad, request.Id);
            if (lista.Estado != ValidacionListaMaterial.Activa)
            {
                throw ValidacionListaMaterial.Conflicto("Solo se pueden retirar listas de materiales activas.");
            }

            lista.Estado = ValidacionListaMaterial.Retirada;
            await _unidad.CompleteAsync();
            return request.Id;
        }
    }
}
