using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo00_Autenticacion;

internal static class RolesDeUsuario
{
    public static async Task<List<string>> ObtenerCodigosAsync(IUnitOfWork unidad, int usuarioId)
    {
        IQueryable<UsuarioRol> enlaces = unidad.Repository<UsuarioRol>().AsQueryable();
        IQueryable<Rol> roles = unidad.Repository<Rol>().AsQueryable();

        return await enlaces.Where(enlace => enlace.UsuarioId == usuarioId)
            .Join(roles, enlace => enlace.RolId, rol => rol.Id, (enlace, rol) => rol.Codigo)
            .OrderBy(codigo => codigo)
            .ToListAsync();
    }
}
