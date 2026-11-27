using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Queries;

public class ObtenerDisponiblesQuery : IRequest<List<ProductoDisponibleDto>>
{
    public int OrdenId { get; set; }
}

public class ObtenerDisponiblesQueryHandler : IRequestHandler<ObtenerDisponiblesQuery, List<ProductoDisponibleDto>>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public ObtenerDisponiblesQueryHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<List<ProductoDisponibleDto>> Handle(ObtenerDisponiblesQuery request, CancellationToken cancellationToken)
    {
        bool ordenExiste = await _unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
            .AnyAsync(orden => orden.Id == request.OrdenId);
        if (!ordenExiste)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La orden de compra {request.OrdenId} no existe.");
        }

        return await OperacionesDevolucionesComerciales.CalcularDisponiblesAsync(_unidadTrabajo, request.OrdenId, null);
    }
}
