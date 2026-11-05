using System.Globalization;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo71_Usuarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo00_Autenticacion;

public class ObtenerPerfilUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerPerfilUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PerfilDto> ExecuteAsync(string? identificadorToken)
    {
        if (!int.TryParse(identificadorToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out int usuarioId))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status401Unauthorized,
                "Debe iniciar sesión para acceder a este recurso.");
        }

        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, usuarioId);
        List<string> roles = await RolesDeUsuario.ObtenerCodigosAsync(_unidad, usuario.Id);

        return new PerfilDto
        {
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Roles = roles
        };
    }
}
