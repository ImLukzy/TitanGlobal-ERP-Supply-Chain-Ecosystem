using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios.Queries;

public class ListarUsuariosQuery : IRequest<PaginaUsuariosDto>
{
    public string? Busqueda { get; set; }

    public int? RolId { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarUsuariosQueryHandler : IRequestHandler<ListarUsuariosQuery, PaginaUsuariosDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarUsuariosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaUsuariosDto> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        ValidarPaginacion(request.Pagina, request.TamanoPagina);
        IQueryable<Usuario> consulta = _unidad.Repository<Usuario>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(usuario => usuario.NombreUsuario.Contains(termino)
                || usuario.NombreCompleto.ToLower().Contains(termino)
                || usuario.Email.Contains(termino));
        }

        if (request.RolId.HasValue)
        {
            int idRol = request.RolId.Value;
            IQueryable<UsuarioRol> enlacesFiltro = _unidad.Repository<UsuarioRol>().AsQueryable();
            consulta = consulta.Where(usuario => enlacesFiltro
                .Any(enlace => enlace.UsuarioId == usuario.Id && enlace.RolId == idRol));
        }

        if (request.Activo.HasValue)
        {
            consulta = consulta.Where(usuario => usuario.Activo == request.Activo.Value);
        }

        int total = await consulta.CountAsync();
        List<UsuarioListadoDto> items = await consulta.OrderBy(usuario => usuario.NombreUsuario)
            .ThenBy(usuario => usuario.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(usuario => new UsuarioListadoDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Activo = usuario.Activo
            })
            .ToListAsync();
        List<int> ids = items.Select(usuario => usuario.Id).ToList();
        IQueryable<UsuarioRol> relacionesPagina = _unidad.Repository<UsuarioRol>().AsQueryable();
        IQueryable<Rol> consultaRolesPagina = _unidad.Repository<Rol>().AsQueryable();
        List<UsuarioRolCodigoDto> roles = await (from enlace in relacionesPagina
                                                 join rol in consultaRolesPagina on enlace.RolId equals rol.Id
                                                 where ids.Contains(enlace.UsuarioId)
                                                 orderby rol.Codigo
                                                 select new UsuarioRolCodigoDto { UsuarioId = enlace.UsuarioId, RolId = enlace.RolId, Codigo = rol.Codigo }).ToListAsync();
        Dictionary<int, List<string>> rolesPorUsuario = roles.GroupBy(enlace => enlace.UsuarioId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(enlace => enlace.Codigo).ToList());
        foreach (UsuarioListadoDto usuario in items)
        {
            if (rolesPorUsuario.TryGetValue(usuario.Id, out List<string>? codigos))
            {
                usuario.Roles = codigos;
            }
        }

        return new PaginaUsuariosDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }
    }
}
