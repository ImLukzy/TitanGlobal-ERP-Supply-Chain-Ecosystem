using MediatR;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Commands;

public class RecibirDevolucionComercialCommand : IRequest<DevolucionDetalleDto>
{
    public int Id { get; set; }
}

public class RecibirDevolucionComercialCommandHandler : IRequestHandler<RecibirDevolucionComercialCommand, DevolucionDetalleDto>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public RecibirDevolucionComercialCommandHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<DevolucionDetalleDto> Handle(RecibirDevolucionComercialCommand request, CancellationToken cancellationToken)
    {
        DevolucionComercial devolucion = await OperacionesDevolucionesComerciales.ObtenerEntidadAsync(_unidadTrabajo, request.Id);
        OperacionesDevolucionesComerciales.ExigirEstado(devolucion, "Aprobada");
        devolucion.Estado = "Recibida";
        devolucion.FechaRecepcion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
        await _unidadTrabajo.CompleteAsync();
        return await OperacionesDevolucionesComerciales.ProyectarDetalleAsync(_unidadTrabajo, devolucion);
    }
}
