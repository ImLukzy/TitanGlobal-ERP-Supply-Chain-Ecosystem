using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Queries;

public class ObtenerRolQuery : IRequest<RolDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerRolQueryHandler : IRequestHandler<ObtenerRolQuery, RolDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerRolQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<RolDetalleDto> Handle(ObtenerRolQuery request, CancellationToken cancellationToken)
    {
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, request.Id);
        List<PermisoRol> permisos = await _unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => permiso.RolId == request.Id).ToListAsync();
        return new RolDetalleDto
        {
            Id = rol.Id,
            Codigo = rol.Codigo,
            Nombre = rol.Nombre,
            Descripcion = rol.Descripcion,
            EsSistema = rol.EsSistema,
            ModulosConLectura = permisos.Count(permiso => permiso.PuedeLeer),
            ModulosConEscritura = permisos.Count(permiso => permiso.PuedeEscribir)
        };
    }
}
