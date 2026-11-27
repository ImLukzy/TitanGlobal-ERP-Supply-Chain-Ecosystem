using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores.Commands;

public class EliminarConductorCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarConductorCommandHandler : IRequestHandler<EliminarConductorCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarConductorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarConductorCommand request, CancellationToken cancellationToken)
    {
        Conductor? conductor = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .FirstOrDefaultAsync(conductor => conductor.Id == request.Id);

        if (conductor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El conductor {request.Id} no existe.");
        }

        _unitOfWork.Repository<Conductor>().Remove(conductor);
        await _unitOfWork.CompleteAsync();
    }
}
