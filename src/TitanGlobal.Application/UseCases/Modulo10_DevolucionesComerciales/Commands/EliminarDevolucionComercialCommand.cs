using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Commands;

public class EliminarDevolucionComercialCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarDevolucionComercialCommandHandler : IRequestHandler<EliminarDevolucionComercialCommand>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public EliminarDevolucionComercialCommandHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task Handle(EliminarDevolucionComercialCommand request, CancellationToken cancellationToken)
    {
        DevolucionComercial devolucion = await OperacionesDevolucionesComerciales.ObtenerEntidadAsync(_unidadTrabajo, request.Id);
        OperacionesDevolucionesComerciales.ExigirEstado(devolucion, "Solicitada");
        List<DetalleDevolucion> lineas = await _unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .Where(detalle => detalle.DevolucionComercialId == request.Id)
            .ToListAsync();
        foreach (DetalleDevolucion linea in lineas)
        {
            _unidadTrabajo.Repository<DetalleDevolucion>().Remove(linea);
        }

        _unidadTrabajo.Repository<DevolucionComercial>().Remove(devolucion);
        await _unidadTrabajo.CompleteAsync();
    }
}
