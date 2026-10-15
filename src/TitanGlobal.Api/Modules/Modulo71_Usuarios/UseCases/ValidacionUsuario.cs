using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo71_Usuarios.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo71_Usuarios.UseCases;

internal static class ValidacionUsuario
{
    private static readonly Regex FormatoNombreUsuario = new Regex(
        "^[a-z0-9._-]{3,30}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FormatoEmail = new Regex(
        "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static (string NombreUsuario, string NombreCompleto, string Email) Normalizar(
        GuardarUsuarioRequestDto solicitud)
    {
        string nombreUsuario = solicitud.NombreUsuario?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre de usuario es obligatorio.");
        }

        if (!FormatoNombreUsuario.IsMatch(nombreUsuario))
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre de usuario solo admite letras minúsculas, números, punto, guion y guion bajo (3 a 30 caracteres).");
        }

        string nombreCompleto = solicitud.NombreCompleto?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre completo es obligatorio.");
        }

        if (nombreCompleto.Length > 100)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre completo no puede superar 100 caracteres.");
        }

        string email = solicitud.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El email es obligatorio.");
        }

        if (email.Length > 150 || !FormatoEmail.IsMatch(email))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El email no tiene un formato válido.");
        }

        return (nombreUsuario, nombreCompleto, email);
    }

    public static async Task ValidarRolesAsync(IUnitOfWork unidad, List<int> rolIds)
    {
        HashSet<int> idsVistos = new HashSet<int>();
        foreach (int rolId in rolIds)
        {
            if (!idsVistos.Add(rolId))
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status400BadRequest,
                    $"El rol {rolId} está repetido.");
            }
        }

        List<int> idsExistentes = await unidad.Repository<Rol>().AsQueryable()
            .Where(rol => rolIds.Contains(rol.Id))
            .Select(rol => rol.Id)
            .ToListAsync();
        foreach (int rolId in rolIds)
        {
            if (!idsExistentes.Contains(rolId))
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status400BadRequest,
                    $"El rol {rolId} no existe.");
            }
        }
    }

    public static async Task ValidarUnicidadAsync(
        IUnitOfWork unidad,
        string nombreUsuario,
        string email,
        int? excluirId)
    {
        bool usuarioExiste = await unidad.Repository<Usuario>().AsQueryable()
            .AnyAsync(usuario => usuario.NombreUsuario == nombreUsuario
                && (!excluirId.HasValue || usuario.Id != excluirId.Value));
        if (usuarioExiste)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status409Conflict,
                $"Ya existe el usuario {nombreUsuario}.");
        }

        bool emailExiste = await unidad.Repository<Usuario>().AsQueryable()
            .AnyAsync(usuario => usuario.Email == email
                && (!excluirId.HasValue || usuario.Id != excluirId.Value));
        if (emailExiste)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status409Conflict,
                $"Ya existe un usuario con el email {email}.");
        }
    }

    public static async Task<Usuario> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        Usuario? usuario = await unidad.Repository<Usuario>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (usuario is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El usuario {id} no existe.");
        }

        return usuario;
    }
}
