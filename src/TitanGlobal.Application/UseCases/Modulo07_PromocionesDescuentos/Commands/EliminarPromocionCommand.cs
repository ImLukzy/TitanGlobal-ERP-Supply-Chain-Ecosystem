using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Commands;

public class EliminarPromocionCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarPromocionCommandHandler : IRequestHandler<EliminarPromocionCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarPromocionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarPromocionCommand request, CancellationToken cancellationToken)
    {
        Promocion? promocion = await _unitOfWork.Repository<Promocion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (promocion is null)
        {
            throw OperacionesPromociones.Error(CodigosEstado.Status404NotFound, $"La promoción {request.Id} no existe.");
        }

        _unitOfWork.Repository<Promocion>().Remove(promocion);
        await _unitOfWork.CompleteAsync();
    }
}
