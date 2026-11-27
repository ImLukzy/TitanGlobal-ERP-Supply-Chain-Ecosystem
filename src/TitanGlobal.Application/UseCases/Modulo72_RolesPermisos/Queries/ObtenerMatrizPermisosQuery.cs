using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Queries;

public class ObtenerMatrizPermisosQuery : IRequest<MatrizPermisosDto>
{
}

public class ObtenerMatrizPermisosQueryHandler : IRequestHandler<ObtenerMatrizPermisosQuery, MatrizPermisosDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerMatrizPermisosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<MatrizPermisosDto> Handle(ObtenerMatrizPermisosQuery request, CancellationToken cancellationToken)
    {
        List<RolResumenDto> roles = await _unidad.Repository<Rol>().AsQueryable()
            .OrderBy(rol => rol.Codigo).ThenBy(rol => rol.Id)
            .Select(rol => new RolResumenDto { Id = rol.Id, Codigo = rol.Codigo }).ToListAsync();
        List<PermisoRol> filas = await _unidad.Repository<PermisoRol>().AsQueryable().ToListAsync();
        Dictionary<(int RolId, int Modulo), PermisoRol> permisos = filas.ToDictionary(permiso => (permiso.RolId, permiso.ModuloNumero));
        List<ModuloMatrizDto> modulos = CatalogoModulos.Todos.Select(modulo => new ModuloMatrizDto
        {
            ModuloNumero = modulo.ModuloNumero,
            ModuloNombre = modulo.ModuloNombre,
            Dominio = modulo.Dominio,
            Permisos = roles.Select(rol => permisos.TryGetValue((rol.Id, modulo.ModuloNumero), out PermisoRol? permiso)
                ? permiso.PuedeEscribir ? "Escritura" : permiso.PuedeLeer ? "Lectura" : "Ninguno"
                : "Ninguno").ToList()
        }).ToList();
        return new MatrizPermisosDto { Roles = roles, Modulos = modulos };
    }
}
