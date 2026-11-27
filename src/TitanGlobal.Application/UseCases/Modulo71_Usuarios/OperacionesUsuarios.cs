using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Application.UseCases.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios;

// Lógica compartida por los commands y queries de Usuarios.
internal static class OperacionesUsuarios
{
    internal static async Task<List<PermisoEfectivoDto>> ObtenerPermisosInternosAsync(IUnitOfWork unidad, Usuario usuario)
    {
        if (!usuario.Activo)
        {
            return CatalogoModulos.Todos.Select(modulo => new PermisoEfectivoDto
            {
                ModuloNumero = modulo.ModuloNumero,
                ModuloNombre = modulo.ModuloNombre,
                Dominio = modulo.Dominio,
                Permiso = "Ninguno"
            }).ToList();
        }

        IQueryable<UsuarioRol> relacionesUsuario = unidad.Repository<UsuarioRol>().AsQueryable();
        IQueryable<Rol> consultaRolesUsuario = unidad.Repository<Rol>().AsQueryable();
        List<UsuarioRolCodigoDto> roles = await (from enlace in relacionesUsuario
                                                 join rol in consultaRolesUsuario on enlace.RolId equals rol.Id
                                                 where enlace.UsuarioId == usuario.Id
                                                 orderby rol.Codigo
                                                 select new UsuarioRolCodigoDto { UsuarioId = enlace.UsuarioId, RolId = enlace.RolId, Codigo = rol.Codigo }).ToListAsync();
        List<int> idsRoles = roles.Select(enlace => enlace.RolId).ToList();
        List<PermisoRol> permisos = await unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => idsRoles.Contains(permiso.RolId))
            .ToListAsync();
        List<PermisoEfectivoDto> resultado = new List<PermisoEfectivoDto>();
        foreach (TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.ModuloCatalogo modulo in CatalogoModulos.Todos)
        {
            List<string> escritura = permisos.Where(permiso => permiso.ModuloNumero == modulo.ModuloNumero && permiso.PuedeEscribir)
                .Select(permiso => roles.Single(rol => rol.RolId == permiso.RolId).Codigo)
                .Distinct()
                .OrderBy(codigo => codigo)
                .ToList();
            List<string> lectura = permisos.Where(permiso => permiso.ModuloNumero == modulo.ModuloNumero && permiso.PuedeLeer)
                .Select(permiso => roles.Single(rol => rol.RolId == permiso.RolId).Codigo)
                .Distinct()
                .OrderBy(codigo => codigo)
                .ToList();
            string nivel = escritura.Count > 0 ? "Escritura" : lectura.Count > 0 ? "Lectura" : "Ninguno";
            resultado.Add(new PermisoEfectivoDto
            {
                ModuloNumero = modulo.ModuloNumero,
                ModuloNombre = modulo.ModuloNombre,
                Dominio = modulo.Dominio,
                Permiso = nivel,
                OtorgadoPor = escritura.Count > 0 ? escritura : nivel == "Lectura" ? lectura : new List<string>()
            });
        }

        return resultado;
    }

    internal static async Task ValidarAdminPosteriorAsync(IUnitOfWork unidad, int? usuarioExcluidoId, bool usuarioActivo, List<int> rolesUsuario)
    {
        bool asignaAdmin = false;
        if (rolesUsuario.Count > 0)
        {
            asignaAdmin = await unidad.Repository<Rol>().AsQueryable()
                .AnyAsync(rol => rolesUsuario.Contains(rol.Id) && rol.Codigo == "ADMIN");
        }

        bool otroAdminActivo = await HayOtroAdminActivoAsync(unidad, usuarioExcluidoId);
        if (!(otroAdminActivo || usuarioActivo && asignaAdmin))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Debe quedar al menos un usuario activo con el rol ADMIN.");
        }
    }

    internal static async Task<bool> HayOtroAdminActivoAsync(IUnitOfWork unidad, int? usuarioExcluidoId)
    {
        return await (from enlace in unidad.Repository<UsuarioRol>().AsQueryable()
                      join usuario in unidad.Repository<Usuario>().AsQueryable() on enlace.UsuarioId equals usuario.Id
                      join rol in unidad.Repository<Rol>().AsQueryable() on enlace.RolId equals rol.Id
                      where rol.Codigo == "ADMIN"
                          && usuario.Activo
                          && (!usuarioExcluidoId.HasValue || usuario.Id != usuarioExcluidoId.Value)
                      select usuario.Id).AnyAsync();
    }

    internal static bool EsDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    internal static ReglaNegocioException CrearConflictoDuplicado(
        ViolacionRestriccionException excepcion,
        string nombreUsuario,
        string email)
    {
        if (excepcion.Restriccion == "IX_Usuarios_Email")
        {
            return new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe un usuario con el email {email}.");
        }

        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe el usuario {nombreUsuario}.");
    }
}

internal sealed class UsuarioRolCodigoDto
{
    public int UsuarioId { get; set; }

    public int RolId { get; set; }

    public string Codigo { get; set; } = string.Empty;
}
