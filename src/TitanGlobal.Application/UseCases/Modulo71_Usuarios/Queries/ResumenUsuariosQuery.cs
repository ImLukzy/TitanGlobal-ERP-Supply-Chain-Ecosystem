using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios.Queries;

public class ResumenUsuariosQuery : IRequest<ResumenUsuariosDto>
{
}

public class ResumenUsuariosQueryHandler : IRequestHandler<ResumenUsuariosQuery, ResumenUsuariosDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenUsuariosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenUsuariosDto> Handle(ResumenUsuariosQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Usuario> usuarios = _unidad.Repository<Usuario>().AsQueryable();
        IQueryable<UsuarioRol> relaciones = _unidad.Repository<UsuarioRol>().AsQueryable();
        int total = await usuarios.CountAsync();
        int activos = await usuarios.CountAsync(usuario => usuario.Activo);
        int sinRol = await usuarios.CountAsync(usuario => !relaciones
            .Any(enlace => enlace.UsuarioId == usuario.Id));
        IQueryable<Rol> consultaRoles = _unidad.Repository<Rol>().AsQueryable();
        List<RolUsuariosDto> porRol = await (from enlace in relaciones
                                             join rol in consultaRoles on enlace.RolId equals rol.Id
                                             group enlace by new { enlace.RolId, rol.Codigo } into grupo
                                             orderby grupo.Key.Codigo
                                             select new RolUsuariosDto
                                             {
                                                 RolId = grupo.Key.RolId,
                                                 Codigo = grupo.Key.Codigo,
                                                 Usuarios = grupo.Select(enlace => enlace.UsuarioId).Distinct().Count()
                                             }).ToListAsync();
        return new ResumenUsuariosDto
        {
            Total = total,
            Activos = activos,
            Inactivos = total - activos,
            SinRol = sinRol,
            PorRol = porRol
        };
    }
}
