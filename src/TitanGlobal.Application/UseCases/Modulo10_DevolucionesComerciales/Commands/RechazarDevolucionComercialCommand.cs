using MediatR;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Commands;

public class RechazarDevolucionComercialCommand : IRequest<DevolucionDetalleDto>
{
    public int Id { get; set; }

    public required RechazarDevolucionRequestDto Solicitud { get; set; }
}

public class RechazarDevolucionComercialCommandHandler : IRequestHandler<RechazarDevolucionComercialCommand, DevolucionDetalleDto>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public RechazarDevolucionComercialCommandHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<DevolucionDetalleDto> Handle(RechazarDevolucionComercialCommand request, CancellationToken cancellationToken)
    {
        DevolucionComercial devolucion = await OperacionesDevolucionesComerciales.ObtenerEntidadAsync(_unidadTrabajo, request.Id);
        OperacionesDevolucionesComerciales.ExigirEstado(devolucion, "Solicitada", "Aprobada");
        devolucion.MotivoRechazo = ValidacionDevolucion.NormalizarMotivoRechazo(request.Solicitud.Motivo);
        devolucion.Estado = "Rechazada";
        devolucion.FechaRechazo = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
        await _unidadTrabajo.CompleteAsync();
        return await OperacionesDevolucionesComerciales.ProyectarDetalleAsync(_unidadTrabajo, devolucion);
    }
}
