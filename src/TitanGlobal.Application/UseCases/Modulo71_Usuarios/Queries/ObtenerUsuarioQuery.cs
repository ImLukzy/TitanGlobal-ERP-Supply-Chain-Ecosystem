using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios.Queries;

public class ObtenerUsuarioQuery : IRequest<UsuarioDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerUsuarioQueryHandler : IRequestHandler<ObtenerUsuarioQuery, UsuarioDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerUsuarioQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<UsuarioDetalleDto> Handle(ObtenerUsuarioQuery request, CancellationToken cancellationToken)
    {
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, request.Id);
        IQueryable<UsuarioRol> relacionesUsuario = _unidad.Repository<UsuarioRol>().AsQueryable();
        IQueryable<Rol> consultaRolesUsuario = _unidad.Repository<Rol>().AsQueryable();
        List<UsuarioRolCodigoDto> roles = await (from enlace in relacionesUsuario
                                                 join rol in consultaRolesUsuario on enlace.RolId equals rol.Id
                                                 where enlace.UsuarioId == request.Id
                                                 orderby rol.Codigo
                                                 select new UsuarioRolCodigoDto { UsuarioId = enlace.UsuarioId, RolId = enlace.RolId, Codigo = rol.Codigo }).ToListAsync();
        List<PermisoEfectivoDto> permisos = await OperacionesUsuarios.ObtenerPermisosInternosAsync(_unidad, usuario);
        return new UsuarioDetalleDto
        {
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email,
            Activo = usuario.Activo,
            FechaAlta = usuario.FechaAlta,
            RolIds = roles.Select(enlace => enlace.RolId).ToList(),
            Roles = roles.Select(enlace => enlace.Codigo).ToList(),
            ModulosConLectura = permisos.Count(permiso => permiso.Permiso == "Lectura" || permiso.Permiso == "Escritura"),
            ModulosConEscritura = permisos.Count(permiso => permiso.Permiso == "Escritura")
        };
    }
}
