using MediatR;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Commands;

public class ResolverReclamoPostventaCommand : IRequest<ReclamoDetalleDto>
{
    public int Id { get; set; }

    public required ResolverReclamoRequestDto Solicitud { get; set; }
}

public class ResolverReclamoPostventaCommandHandler : IRequestHandler<ResolverReclamoPostventaCommand, ReclamoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResolverReclamoPostventaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReclamoDetalleDto> Handle(ResolverReclamoPostventaCommand request, CancellationToken cancellationToken)
    {
        ReclamoPostventa reclamo = await OperacionesReclamosPostventa.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesReclamosPostventa.ExigirEstado(reclamo, "Abierto", "EnProceso");
        reclamo.Resolucion = ValidacionReclamo.NormalizarResolucion(request.Solicitud.Resolucion);
        reclamo.Estado = "Resuelto";
        reclamo.FechaResolucion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return await OperacionesReclamosPostventa.ProyectarDetalleAsync(_unitOfWork, reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }
}
