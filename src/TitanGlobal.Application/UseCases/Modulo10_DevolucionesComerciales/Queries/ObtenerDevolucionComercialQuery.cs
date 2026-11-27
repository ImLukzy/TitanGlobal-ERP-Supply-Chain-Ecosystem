using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Queries;

public class ObtenerDevolucionComercialQuery : IRequest<DevolucionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerDevolucionComercialQueryHandler : IRequestHandler<ObtenerDevolucionComercialQuery, DevolucionDetalleDto>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public ObtenerDevolucionComercialQueryHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<DevolucionDetalleDto> Handle(ObtenerDevolucionComercialQuery request, CancellationToken cancellationToken)
    {
        DevolucionComercial? devolucion = await _unidadTrabajo.Repository<DevolucionComercial>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (devolucion is null)
        {
            throw OperacionesDevolucionesComerciales.CrearNoEncontrado(request.Id);
        }

        return await OperacionesDevolucionesComerciales.ProyectarDetalleAsync(_unidadTrabajo, devolucion);
    }
}
