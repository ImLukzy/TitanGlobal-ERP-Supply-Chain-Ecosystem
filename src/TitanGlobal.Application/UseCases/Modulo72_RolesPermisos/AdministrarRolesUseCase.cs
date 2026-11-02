using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos;

public class AdministrarRolesUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarRolesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaRolesDto> ListarAsync(string? busqueda, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<Rol> consulta = _unidad.Repository<Rol>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(rol => rol.Codigo.ToLower().Contains(termino) || rol.Nombre.ToLower().Contains(termino));
        }

        int total = await consulta.CountAsync();
        List<RolListadoDto> roles = await consulta.OrderBy(rol => rol.Codigo).ThenBy(rol => rol.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
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

        return new PaginaRolesDto { Items = roles, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    public async Task<RolDetalleDto> ObtenerAsync(int id)
    {
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, id);
        List<PermisoRol> permisos = await _unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => permiso.RolId == id).ToListAsync();
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

    public async Task<int> CrearAsync(GuardarRolRequestDto solicitud)
    {
        (string codigo, string nombre, string? descripcion) = ValidacionRol.Normalizar(solicitud);
        await ValidacionRol.ValidarCodigoUnicoAsync(_unidad, codigo, null);
        Rol rol = new Rol { Codigo = codigo, Nombre = nombre, Descripcion = descripcion, EsSistema = false };
        await _unidad.Repository<Rol>().AddAsync(rol);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsCodigoDuplicado(excepcion))
        {
            throw ConflictoCodigo(codigo);
        }

        return rol.Id;
    }

    public async Task<int> ActualizarAsync(int id, GuardarRolRequestDto solicitud)
    {
        (string codigo, string nombre, string? descripcion) = ValidacionRol.Normalizar(solicitud);
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, id);
        await ValidacionRol.ValidarCodigoUnicoAsync(_unidad, codigo, id);
        if (rol.EsSistema && rol.Codigo != codigo)
        {
            throw ConflictoSistema(rol.Codigo);
        }

        rol.Codigo = codigo;
        rol.Nombre = nombre;
        rol.Descripcion = descripcion;
        _unidad.Repository<Rol>().Update(rol);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsCodigoDuplicado(excepcion))
        {
            throw ConflictoCodigo(codigo);
        }

        return rol.Id;
    }

    public async Task<List<PermisoModuloDto>> ObtenerPermisosAsync(int id)
    {
        await ValidacionRol.ObtenerAsync(_unidad, id);
        List<PermisoRol> permisos = await _unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => permiso.RolId == id).ToListAsync();
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

    public async Task<List<PermisoModuloDto>> GuardarPermisosAsync(int id, GuardarPermisosRequestDto solicitud)
    {
        ValidacionRol.ValidarPermisos(solicitud);
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, id);
        List<PermisoRol> actuales = await _unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => permiso.RolId == id).ToListAsync();
        if (rol.EsSistema && actuales.Any(actual =>
            !solicitud.Permisos.Any(solicitado => solicitado.ModuloNumero == actual.ModuloNumero
                && (!actual.PuedeLeer || solicitado.PuedeLeer)
                && (!actual.PuedeEscribir || solicitado.PuedeEscribir))))
        {
            throw ConflictoSistema(rol.Codigo);
        }

        foreach (PermisoRol actual in actuales)
        {
            _unidad.Repository<PermisoRol>().Remove(actual);
        }

        foreach (PermisoSolicitadoDto solicitado in solicitud.Permisos)
        {
            if (solicitado.PuedeLeer || solicitado.PuedeEscribir)
            {
                await _unidad.Repository<PermisoRol>().AddAsync(new PermisoRol
                {
                    RolId = id,
                    ModuloNumero = solicitado.ModuloNumero,
                    PuedeLeer = solicitado.PuedeLeer,
                    PuedeEscribir = solicitado.PuedeEscribir
                });
            }
        }

        await _unidad.CompleteAsync();
        return await ObtenerPermisosAsync(id);
    }

    public async Task<MatrizPermisosDto> MatrizAsync()
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

    private static bool EsCodigoDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    private static ReglaNegocioException ConflictoCodigo(string codigo)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe un rol con el código {codigo}.");
    }

    private static ReglaNegocioException ConflictoSistema(string codigo)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"El rol {codigo} es de sistema y no se puede modificar de esa forma.");
    }
}
