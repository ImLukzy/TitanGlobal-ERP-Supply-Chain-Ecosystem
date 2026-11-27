using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores.Queries;

public class ObtenerProveedorQuery : IRequest<ProveedorDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerProveedorQueryHandler : IRequestHandler<ObtenerProveedorQuery, ProveedorDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerProveedorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProveedorDetalleDto> Handle(ObtenerProveedorQuery request, CancellationToken cancellationToken)
    {
        ProveedorDetalleDto? proveedor = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.Id)
            .Select(entidad => new ProveedorDetalleDto
            {
                Id = entidad.Id,
                RazonSocial = entidad.RazonSocial,
                Ruc = entidad.Ruc,
                Pais = entidad.Pais,
                Email = entidad.Email,
                Telefono = entidad.Telefono,
                Activo = entidad.Activo
            })
            .SingleOrDefaultAsync();

        if (proveedor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El proveedor {request.Id} no existe.");
        }

        return proveedor;
    }
}
