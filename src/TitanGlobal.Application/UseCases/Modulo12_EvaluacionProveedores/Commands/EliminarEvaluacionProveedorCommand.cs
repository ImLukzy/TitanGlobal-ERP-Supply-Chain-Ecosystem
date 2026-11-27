using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores.Commands;

public class EliminarEvaluacionProveedorCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarEvaluacionProveedorCommandHandler : IRequestHandler<EliminarEvaluacionProveedorCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarEvaluacionProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarEvaluacionProveedorCommand request, CancellationToken cancellationToken)
    {
        EvaluacionProveedor? evaluacion = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (evaluacion is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La evaluación {request.Id} no existe.");
        }

        _unitOfWork.Repository<EvaluacionProveedor>().Remove(evaluacion);
        await _unitOfWork.CompleteAsync();
    }
}
