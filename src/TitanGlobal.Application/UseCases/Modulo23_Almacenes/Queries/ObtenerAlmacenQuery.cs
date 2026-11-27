using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes.Queries;

public class ObtenerAlmacenQuery : IRequest<AlmacenDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerAlmacenQueryHandler : IRequestHandler<ObtenerAlmacenQuery, AlmacenDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerAlmacenQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AlmacenDetalleDto> Handle(ObtenerAlmacenQuery request, CancellationToken cancellationToken)
    {
        AlmacenDetalleDto? almacen = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.Id)
            .Select(entidad => new AlmacenDetalleDto
            {
                Id = entidad.Id,
                Codigo = entidad.Codigo,
                Nombre = entidad.Nombre,
                Tipo = entidad.Tipo,
                Direccion = entidad.Direccion,
                Ciudad = entidad.Ciudad,
                CapacidadM3 = entidad.CapacidadM3,
                Responsable = entidad.Responsable,
                Activo = entidad.Activo
            })
            .SingleOrDefaultAsync();
        if (almacen is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El almacén {request.Id} no existe.");
        }

        return almacen;
    }
}
