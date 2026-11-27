using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje.Commands;

public class EliminarZonaAlmacenajeCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarZonaAlmacenajeCommandHandler : IRequestHandler<EliminarZonaAlmacenajeCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarZonaAlmacenajeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarZonaAlmacenajeCommand request, CancellationToken cancellationToken)
    {
        ZonaAlmacenaje? zona = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (zona is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La zona {request.Id} no existe.");
        }

        _unitOfWork.Repository<ZonaAlmacenaje>().Remove(zona);
        await _unitOfWork.CompleteAsync();
    }
}
