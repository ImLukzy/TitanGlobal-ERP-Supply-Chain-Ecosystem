using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Queries;

public class ObtenerOrdenAbastecimientoQuery : IRequest<OrdenAbastecimientoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerOrdenAbastecimientoQueryHandler : IRequestHandler<ObtenerOrdenAbastecimientoQuery, OrdenAbastecimientoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerOrdenAbastecimientoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoDetalleDto> Handle(ObtenerOrdenAbastecimientoQuery request, CancellationToken cancellationToken)
    {
        DateTime hoyUtc = DateTime.UtcNow.Date;
        OrdenAbastecimientoDetalleDto? orden = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.Id)
            // PATRÓN LINQ 10: proyección jerárquica de la orden y sus líneas.
            .Select(ProyeccionOrdenAbastecimiento.CrearSelector(hoyUtc))
            .SingleOrDefaultAsync();
        if (orden is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La orden de abastecimiento {request.Id} no existe.");
        }

        return orden;
    }
}
