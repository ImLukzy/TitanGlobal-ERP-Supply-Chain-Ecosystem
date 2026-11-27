using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Queries;

public class ListarRolesQuery : IRequest<PaginaRolesDto>
{
    public string? Busqueda { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarRolesQueryHandler : IRequestHandler<ListarRolesQuery, PaginaRolesDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarRolesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaRolesDto> Handle(ListarRolesQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<Rol> consulta = _unidad.Repository<Rol>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(rol => rol.Codigo.ToLower().Contains(termino) || rol.Nombre.ToLower().Contains(termino));
        }

        int total = await consulta.CountAsync();
        List<RolListadoDto> roles = await consulta.OrderBy(rol => rol.Codigo).ThenBy(rol => rol.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina).Take(request.TamanoPagina)
            .Select(rol => new RolListadoDto
            {
                Id = rol.Id,
                Codigo = rol.Codigo,
                Nombre = rol.Nombre,
                EsSistema = rol.EsSistema
            }).ToListAsync();
        List<int> ids = roles.Select(rol => rol.Id).ToList();
        // PATRÓN LINQ 6: agrupar y contar permisos en una consulta adicional fija.
        List<PermisoRol> filas = await _unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => ids.Contains(permiso.RolId)).ToListAsync();
        Dictionary<int, List<PermisoRol>> porRol = filas.GroupBy(permiso => permiso.RolId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());
        foreach (RolListadoDto rol in roles)
        {
            if (porRol.TryGetValue(rol.Id, out List<PermisoRol>? permisos))
            {
                rol.ModulosConLectura = permisos.Count(permiso => permiso.PuedeLeer);
                rol.ModulosConEscritura = permisos.Count(permiso => permiso.PuedeEscribir);
            }
        }

        return new PaginaRolesDto { Items = roles, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
