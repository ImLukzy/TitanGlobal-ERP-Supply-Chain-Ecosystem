using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Commands;

public class EliminarRutaDespachoCommand : IRequest<int>
{
    public int Id { get; set; }
}

public class EliminarRutaDespachoCommandHandler : IRequestHandler<EliminarRutaDespachoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarRutaDespachoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(EliminarRutaDespachoCommand request, CancellationToken cancellationToken)
    {
        RutaDespacho? ruta = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().FirstOrDefaultAsync(r => r.Id == request.Id);
        if (ruta is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La ruta de despacho {request.Id} no existe."); }
        _unitOfWork.Repository<RutaDespacho>().Remove(ruta);
        try { await _unitOfWork.CompleteAsync(); }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar la ruta de despacho porque está en uso."); }
        return request.Id;
    }
}
