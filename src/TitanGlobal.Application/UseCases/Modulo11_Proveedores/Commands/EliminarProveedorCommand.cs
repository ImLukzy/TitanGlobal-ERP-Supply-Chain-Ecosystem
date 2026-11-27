using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores.Commands;

public class EliminarProveedorCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarProveedorCommandHandler : IRequestHandler<EliminarProveedorCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarProveedorCommand request, CancellationToken cancellationToken)
    {
        Proveedor? proveedor = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (proveedor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El proveedor {request.Id} no existe.");
        }

        _unitOfWork.Repository<Proveedor>().Remove(proveedor);
        await _unitOfWork.CompleteAsync();
    }
}
