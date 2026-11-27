using MediatR;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Commands;

public class RechazarReclamoPostventaCommand : IRequest<ReclamoDetalleDto>
{
    public int Id { get; set; }

    public required RechazarReclamoRequestDto Solicitud { get; set; }
}

public class RechazarReclamoPostventaCommandHandler : IRequestHandler<RechazarReclamoPostventaCommand, ReclamoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public RechazarReclamoPostventaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReclamoDetalleDto> Handle(RechazarReclamoPostventaCommand request, CancellationToken cancellationToken)
    {
        ReclamoPostventa reclamo = await OperacionesReclamosPostventa.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesReclamosPostventa.ExigirEstado(reclamo, "Abierto", "EnProceso");
        reclamo.MotivoRechazo = ValidacionReclamo.NormalizarMotivo(request.Solicitud.Motivo);
        reclamo.Estado = "Rechazado";
        reclamo.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await OperacionesReclamosPostventa.ProyectarDetalleAsync(_unitOfWork, reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }
}
