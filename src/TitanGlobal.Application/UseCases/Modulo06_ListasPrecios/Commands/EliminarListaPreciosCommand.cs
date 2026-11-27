using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Commands;

public class EliminarListaPreciosCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarListaPreciosCommandHandler : IRequestHandler<EliminarListaPreciosCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarListaPreciosCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarListaPreciosCommand request, CancellationToken cancellationToken)
    {
        ListaPrecios? lista = await _unitOfWork.Repository<ListaPrecios>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (lista is null)
        {
            throw OperacionesListasPrecios.Error(CodigosEstado.Status404NotFound, $"La lista de precios {request.Id} no existe.");
        }

        _unitOfWork.Repository<ListaPrecios>().Remove(lista);
        await _unitOfWork.CompleteAsync();
    }
}
