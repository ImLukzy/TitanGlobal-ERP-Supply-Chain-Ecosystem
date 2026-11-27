using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes.Commands;

public class EliminarAlmacenCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarAlmacenCommandHandler : IRequestHandler<EliminarAlmacenCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarAlmacenCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarAlmacenCommand request, CancellationToken cancellationToken)
    {
        Almacen? almacen = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (almacen is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El almacén {request.Id} no existe.");
        }

        bool tieneZonas = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .AnyAsync(zona => zona.AlmacenId == request.Id);
        if (tieneZonas)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"No se puede eliminar el almacén {request.Id} porque tiene zonas.");
        }

        _unitOfWork.Repository<Almacen>().Remove(almacen);
        await _unitOfWork.CompleteAsync();
    }
}
