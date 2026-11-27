using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Commands;

public class CancelarFleteTerceroCommand : IRequest<int>
{
    public int Id { get; set; }

    public required CancelarFleteTerceroRequestDto Solicitud { get; set; }
}

public class CancelarFleteTerceroCommandHandler : IRequestHandler<CancelarFleteTerceroCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelarFleteTerceroCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CancelarFleteTerceroCommand request, CancellationToken cancellationToken)
    {
        FleteTercero? flete = await _unitOfWork.Repository<FleteTercero>().AsQueryable().FirstOrDefaultAsync(f => f.Id == request.Id);
        if (flete is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El flete {request.Id} no existe."); }
        if (flete.Estado == "Cancelado") { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, $"El flete {request.Id} ya está cancelado."); }
        string motivo = ValidacionLogisticaExterna.Obligatorio(request.Solicitud.Motivo, 500, "El motivo de cancelación es obligatorio.", "El motivo de cancelación no puede superar 500 caracteres.");
        flete.Estado = "Cancelado";
        flete.MotivoCancelacion = motivo;
        flete.FechaCancelacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<FleteTercero>().Update(flete);
        await _unitOfWork.CompleteAsync();
        return request.Id;
    }
}
