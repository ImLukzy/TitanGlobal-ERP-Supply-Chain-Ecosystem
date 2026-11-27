using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Commands;

public class EliminarSegmentoClienteCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarSegmentoClienteCommandHandler : IRequestHandler<EliminarSegmentoClienteCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarSegmentoClienteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarSegmentoClienteCommand request, CancellationToken cancellationToken)
    {
        SegmentoCliente? segmento = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (segmento is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {request.Id} no existe.");
        }

        _unitOfWork.Repository<SegmentoCliente>().Remove(segmento);
        await _unitOfWork.CompleteAsync();
    }
}
