using MediatR;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Commands;

public class CerrarReclamoPostventaCommand : IRequest<ReclamoDetalleDto>
{
    public int Id { get; set; }
}

public class CerrarReclamoPostventaCommandHandler : IRequestHandler<CerrarReclamoPostventaCommand, ReclamoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CerrarReclamoPostventaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReclamoDetalleDto> Handle(CerrarReclamoPostventaCommand request, CancellationToken cancellationToken)
    {
        ReclamoPostventa reclamo = await OperacionesReclamosPostventa.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesReclamosPostventa.ExigirEstado(reclamo, "Resuelto");
        reclamo.Estado = "Cerrado";
        reclamo.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await OperacionesReclamosPostventa.ProyectarDetalleAsync(_unitOfWork, reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }
}
