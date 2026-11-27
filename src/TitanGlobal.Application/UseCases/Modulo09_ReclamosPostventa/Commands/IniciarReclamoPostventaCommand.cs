using MediatR;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Commands;

public class IniciarReclamoPostventaCommand : IRequest<ReclamoDetalleDto>
{
    public int Id { get; set; }
}

public class IniciarReclamoPostventaCommandHandler : IRequestHandler<IniciarReclamoPostventaCommand, ReclamoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public IniciarReclamoPostventaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReclamoDetalleDto> Handle(IniciarReclamoPostventaCommand request, CancellationToken cancellationToken)
    {
        ReclamoPostventa reclamo = await OperacionesReclamosPostventa.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesReclamosPostventa.ExigirEstado(reclamo, "Abierto");
        reclamo.Estado = "EnProceso";
        reclamo.FechaInicio = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await OperacionesReclamosPostventa.ProyectarDetalleAsync(_unitOfWork, reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }
}
