using MediatR;
using System.Globalization;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo71_Usuarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo00_Autenticacion.Queries;

public class ObtenerPerfilQuery : IRequest<PerfilDto>
{
    public string? IdentificadorToken { get; set; }
}

public class ObtenerPerfilQueryHandler : IRequestHandler<ObtenerPerfilQuery, PerfilDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerPerfilQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PerfilDto> Handle(ObtenerPerfilQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.IdentificadorToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out int usuarioId))
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
