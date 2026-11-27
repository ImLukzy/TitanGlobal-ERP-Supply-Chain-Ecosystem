using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Commands;

public class EliminarVehiculoFlotaCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarVehiculoFlotaCommandHandler : IRequestHandler<EliminarVehiculoFlotaCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarVehiculoFlotaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarVehiculoFlotaCommand request, CancellationToken cancellationToken)
    {
        VehiculoFlota? vehiculo = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .FirstOrDefaultAsync(v => v.Id == request.Id);

        if (vehiculo is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El vehículo {request.Id} no existe.");
        }

        _unitOfWork.Repository<VehiculoFlota>().Remove(vehiculo);
        await _unitOfWork.CompleteAsync();
    }
}
