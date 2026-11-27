using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Queries;

public class ObtenerPermisosQuery : IRequest<List<PermisoModuloDto>>
{
    public int Id { get; set; }
}

public class ObtenerPermisosQueryHandler : IRequestHandler<ObtenerPermisosQuery, List<PermisoModuloDto>>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerPermisosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<List<PermisoModuloDto>> Handle(ObtenerPermisosQuery request, CancellationToken cancellationToken)
    {
        await ValidacionRol.ObtenerAsync(_unidad, request.Id);
        List<PermisoRol> permisos = await _unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => permiso.RolId == request.Id).ToListAsync();
        Dictionary<int, PermisoRol> porModulo = permisos.ToDictionary(permiso => permiso.ModuloNumero);
        return CatalogoModulos.Todos.Select(modulo =>
        {
            bool existe = porModulo.TryGetValue(modulo.ModuloNumero, out PermisoRol? permiso);
            return new PermisoModuloDto
            {
                ModuloNumero = modulo.ModuloNumero,
                ModuloNombre = modulo.ModuloNombre,
                Dominio = modulo.Dominio,
                PuedeLeer = existe && permiso!.PuedeLeer,
                PuedeEscribir = existe && permiso!.PuedeEscribir
            };
        }).ToList();
    }
}
