using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Commands;

public class EliminarTransportistaTerceroCommand : IRequest<int>
{
    public int Id { get; set; }
}

public class EliminarTransportistaTerceroCommandHandler : IRequestHandler<EliminarTransportistaTerceroCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarTransportistaTerceroCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(EliminarTransportistaTerceroCommand request, CancellationToken cancellationToken)
    {
        TransportistaTercero? entidad = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable().FirstOrDefaultAsync(t => t.Id == request.Id);
        if (entidad is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El transportista {request.Id} no existe."); }
        if (await _unitOfWork.Repository<FleteTercero>().AsQueryable().AnyAsync(f => f.TransportistaTerceroId == request.Id))
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar un transportista con fletes registrados.");
        }
        _unitOfWork.Repository<TransportistaTercero>().Remove(entidad);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar un transportista con fletes registrados.");
        }
        return request.Id;
    }
}
