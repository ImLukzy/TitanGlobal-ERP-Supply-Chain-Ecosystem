using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes.Commands;

public class EliminarClienteCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarClienteCommandHandler : IRequestHandler<EliminarClienteCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarClienteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarClienteCommand request, CancellationToken cancellationToken)
    {
        Cliente? cliente = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (cliente is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El cliente {request.Id} no existe.");
        }

        bool tieneOrdenes = await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .AnyAsync(orden => orden.ClienteId == request.Id);
        if (tieneOrdenes)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"No se puede eliminar el cliente {request.Id} porque tiene órdenes de compra.");
        }

        _unitOfWork.Repository<Cliente>().Remove(cliente);
        await _unitOfWork.CompleteAsync();
    }
}
