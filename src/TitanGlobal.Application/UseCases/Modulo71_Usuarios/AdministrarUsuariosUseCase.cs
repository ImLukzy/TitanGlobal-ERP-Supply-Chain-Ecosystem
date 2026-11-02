using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Application.UseCases.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios;

public class AdministrarUsuariosUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarUsuariosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaUsuariosDto> ListarAsync(
        string? busqueda,
        int? rolId,
        bool? activo,
        int pagina,
        int tamanoPagina)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        IQueryable<Usuario> consulta = _unidad.Repository<Usuario>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(usuario => usuario.NombreUsuario.Contains(termino)
                || usuario.NombreCompleto.ToLower().Contains(termino)
                || usuario.Email.Contains(termino));
        }

        if (rolId.HasValue)
        {
            int idRol = rolId.Value;
            IQueryable<UsuarioRol> enlacesFiltro = _unidad.Repository<UsuarioRol>().AsQueryable();
            consulta = consulta.Where(usuario => enlacesFiltro
                .Any(enlace => enlace.UsuarioId == usuario.Id && enlace.RolId == idRol));
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(usuario => usuario.Activo == activo.Value);
        }

        int total = await consulta.CountAsync();
        List<UsuarioListadoDto> items = await consulta.OrderBy(usuario => usuario.NombreUsuario)
            .ThenBy(usuario => usuario.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<UsuarioDetalleDto> ObtenerAsync(int id)
    {
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, id);
        IQueryable<UsuarioRol> relacionesUsuario = _unidad.Repository<UsuarioRol>().AsQueryable();
        IQueryable<Rol> consultaRolesUsuario = _unidad.Repository<Rol>().AsQueryable();
        List<UsuarioRolCodigoDto> roles = await (from enlace in relacionesUsuario
                                                 join rol in consultaRolesUsuario on enlace.RolId equals rol.Id
                                                 where enlace.UsuarioId == id
                                                 orderby rol.Codigo
                                                 select new UsuarioRolCodigoDto { UsuarioId = enlace.UsuarioId, RolId = enlace.RolId, Codigo = rol.Codigo }).ToListAsync();
        List<PermisoEfectivoDto> permisos = await ObtenerPermisosInternosAsync(usuario);
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

    public async Task<int> CrearAsync(GuardarUsuarioRequestDto solicitud)
    {
        (string nombreUsuario, string nombreCompleto, string email) = ValidacionUsuario.Normalizar(solicitud);
        await ValidacionUsuario.ValidarRolesAsync(_unidad, solicitud.RolIds);
        await ValidacionUsuario.ValidarUnicidadAsync(_unidad, nombreUsuario, email, null);
        await ValidarAdminPosteriorAsync(null, solicitud.Activo, solicitud.RolIds);
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        Usuario usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto,
            Email = email,
            Activo = solicitud.Activo,
            FechaAlta = DateTime.UtcNow
        };
        await _unidad.Repository<Usuario>().AddAsync(usuario);
        try
        {
            await _unidad.CompleteAsync();
            foreach (int rolId in solicitud.RolIds)
            {
                await _unidad.Repository<UsuarioRol>().AddAsync(new UsuarioRol
                {
                    UsuarioId = usuario.Id,
                    RolId = rolId
                });
            }

            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (EsDuplicado(excepcion))
        {
            throw CrearConflictoDuplicado(excepcion, nombreUsuario, email);
        }

        return usuario.Id;
    }

    public async Task<int> ActualizarAsync(int id, GuardarUsuarioRequestDto solicitud)
    {
        (string nombreUsuario, string nombreCompleto, string email) = ValidacionUsuario.Normalizar(solicitud);
        await ValidacionUsuario.ValidarRolesAsync(_unidad, solicitud.RolIds);
        await ValidacionUsuario.ValidarUnicidadAsync(_unidad, nombreUsuario, email, id);
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, id);
        await ValidarAdminPosteriorAsync(id, solicitud.Activo, solicitud.RolIds);
        List<UsuarioRol> rolesActuales = await _unidad.Repository<UsuarioRol>().AsQueryable()
            .Where(enlace => enlace.UsuarioId == id)
            .ToListAsync();
        usuario.NombreUsuario = nombreUsuario;
        usuario.NombreCompleto = nombreCompleto;
        usuario.Email = email;
        usuario.Activo = solicitud.Activo;
        _unidad.Repository<Usuario>().Update(usuario);
        foreach (UsuarioRol enlace in rolesActuales)
        {
            _unidad.Repository<UsuarioRol>().Remove(enlace);
        }

        foreach (int rolId in solicitud.RolIds)
        {
            await _unidad.Repository<UsuarioRol>().AddAsync(new UsuarioRol { UsuarioId = id, RolId = rolId });
        }

        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsDuplicado(excepcion))
        {
            throw CrearConflictoDuplicado(excepcion, nombreUsuario, email);
        }

        return usuario.Id;
    }

    public async Task EliminarAsync(int id)
    {
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, id);
        List<int> rolIds = await _unidad.Repository<UsuarioRol>().AsQueryable()
            .Where(enlace => enlace.UsuarioId == id)
            .Select(enlace => enlace.RolId)
            .ToListAsync();
        await ValidarAdminPosteriorAsync(id, false, rolIds);
        _unidad.Repository<Usuario>().Remove(usuario);
        await _unidad.CompleteAsync();
    }

    public async Task<List<PermisoEfectivoDto>> PermisosAsync(int id)
    {
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, id);
        return await ObtenerPermisosInternosAsync(usuario);
    }

    public async Task<ResumenUsuariosDto> ResumenAsync()
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

    private async Task<List<PermisoEfectivoDto>> ObtenerPermisosInternosAsync(Usuario usuario)
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

        IQueryable<UsuarioRol> relacionesUsuario = _unidad.Repository<UsuarioRol>().AsQueryable();
        IQueryable<Rol> consultaRolesUsuario = _unidad.Repository<Rol>().AsQueryable();
        List<UsuarioRolCodigoDto> roles = await (from enlace in relacionesUsuario
                                                 join rol in consultaRolesUsuario on enlace.RolId equals rol.Id
                                                 where enlace.UsuarioId == usuario.Id
                                                 orderby rol.Codigo
                                                 select new UsuarioRolCodigoDto { UsuarioId = enlace.UsuarioId, RolId = enlace.RolId, Codigo = rol.Codigo }).ToListAsync();
        List<int> idsRoles = roles.Select(enlace => enlace.RolId).ToList();
        List<PermisoRol> permisos = await _unidad.Repository<PermisoRol>().AsQueryable()
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

    private async Task ValidarAdminPosteriorAsync(int? usuarioExcluidoId, bool usuarioActivo, List<int> rolesUsuario)
    {
        bool asignaAdmin = false;
        if (rolesUsuario.Count > 0)
        {
            asignaAdmin = await _unidad.Repository<Rol>().AsQueryable()
                .AnyAsync(rol => rolesUsuario.Contains(rol.Id) && rol.Codigo == "ADMIN");
        }

        bool otroAdminActivo = await HayOtroAdminActivoAsync(usuarioExcluidoId);
        if (!(otroAdminActivo || usuarioActivo && asignaAdmin))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Debe quedar al menos un usuario activo con el rol ADMIN.");
        }
    }

    private async Task<bool> HayOtroAdminActivoAsync(int? usuarioExcluidoId)
    {
        return await (from enlace in _unidad.Repository<UsuarioRol>().AsQueryable()
                      join usuario in _unidad.Repository<Usuario>().AsQueryable() on enlace.UsuarioId equals usuario.Id
                      join rol in _unidad.Repository<Rol>().AsQueryable() on enlace.RolId equals rol.Id
                      where rol.Codigo == "ADMIN"
                          && usuario.Activo
                          && (!usuarioExcluidoId.HasValue || usuario.Id != usuarioExcluidoId.Value)
                      select usuario.Id).AnyAsync();
    }

    private sealed class UsuarioRolCodigoDto
    {
        public int UsuarioId { get; set; }

        public int RolId { get; set; }

        public string Codigo { get; set; } = string.Empty;
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

    private static bool EsDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    private static ReglaNegocioException CrearConflictoDuplicado(
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
