using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Modules.Modulo72_RolesPermisos.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;
using TitanGlobal.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace TitanGlobal.Api.Modules.Modulo72_RolesPermisos.UseCases;

internal static class ValidacionRol
{
    private static readonly Regex FormatoCodigo = new Regex("^[A-Z0-9_-]{2,30}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static (string Codigo, string Nombre, string? Descripcion) Normalizar(GuardarRolRequestDto solicitud)
    {
        string codigo = solicitud.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El código es obligatorio.");
        }

        if (!FormatoCodigo.IsMatch(codigo))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El código solo admite letras, números, guiones y guion bajo (2 a 30 caracteres).");
        }

        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El nombre del rol es obligatorio.");
        }

        if (nombre.Length > 100)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El nombre del rol no puede superar 100 caracteres.");
        }

        string? descripcion = solicitud.Descripcion?.Trim();
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            descripcion = null;
        }

        if (descripcion?.Length > 300)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La descripción no puede superar 300 caracteres.");
        }

        return (codigo, nombre, descripcion);
    }

    public static async Task ValidarCodigoUnicoAsync(IUnitOfWork unidad, string codigo, int? excluirId)
    {
        bool existe = await unidad.Repository<Rol>().AsQueryable().AnyAsync(rol => rol.Codigo == codigo && (!excluirId.HasValue || rol.Id != excluirId.Value));
        if (existe)
        {
            throw new ReglaNegocioException(StatusCodes.Status409Conflict, $"Ya existe un rol con el código {codigo}.");
        }
    }

    public static async Task<Rol> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        Rol? rol = await unidad.Repository<Rol>().AsQueryable().SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (rol is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El rol {id} no existe.");
        }

        return rol;
    }

    public static void ValidarPermisos(GuardarPermisosRequestDto solicitud)
    {
        HashSet<int> vistos = new HashSet<int>();
        foreach (PermisoSolicitadoDto permiso in solicitud.Permisos)
        {
            if (permiso.ModuloNumero < 1 || permiso.ModuloNumero > 80)
            {
                throw new ReglaNegocioException(StatusCodes.Status400BadRequest, $"El módulo {permiso.ModuloNumero} no existe.");
            }

            if (!vistos.Add(permiso.ModuloNumero))
            {
                throw new ReglaNegocioException(StatusCodes.Status400BadRequest, $"El módulo {permiso.ModuloNumero} está repetido.");
            }

            if (permiso.PuedeEscribir && !permiso.PuedeLeer)
            {
                throw new ReglaNegocioException(StatusCodes.Status400BadRequest, $"El módulo {permiso.ModuloNumero} no puede tener escritura sin lectura.");
            }
        }
    }
}
