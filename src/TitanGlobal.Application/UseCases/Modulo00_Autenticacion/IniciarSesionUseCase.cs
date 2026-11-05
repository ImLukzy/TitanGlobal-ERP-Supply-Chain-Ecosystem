using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.Services;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo00_Autenticacion;

public class IniciarSesionUseCase
{
    private const string MensajeCredenciales = "Usuario o contraseña incorrectos.";

    // Hash BCrypt de una contraseña descartada: se compara contra él cuando el usuario no existe
    // para que el tiempo de respuesta no revele qué usuarios existen.
    private const string HashSustituto = "$2a$11$s53gKd79mBMrMGywk/ySEefCYwC2ZvrmDoSTFHa9v6kNUHhU3mXuW";

    private readonly IUnitOfWork _unidad;
    private readonly IServicioContrasenas _contrasenas;
    private readonly IServicioTokens _tokens;

    public IniciarSesionUseCase(IUnitOfWork unidad, IServicioContrasenas contrasenas, IServicioTokens tokens)
    {
        _unidad = unidad;
        _contrasenas = contrasenas;
        _tokens = tokens;
    }

    public async Task<SesionDto> ExecuteAsync(IniciarSesionRequestDto? solicitud)
    {
        string nombreUsuario = solicitud?.NombreUsuario?.Trim().ToLowerInvariant() ?? string.Empty;
        string contrasena = solicitud?.Contrasena ?? string.Empty;
        if (nombreUsuario.Length == 0 || contrasena.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El usuario y la contraseña son obligatorios.");
        }

        Usuario? usuario = await _unidad.Repository<Usuario>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.NombreUsuario == nombreUsuario);
        string hash = usuario?.ContrasenaHash ?? HashSustituto;
        bool contrasenaValida = _contrasenas.Verificar(contrasena, hash);
        if (usuario is null || usuario.ContrasenaHash is null || !usuario.Activo || !contrasenaValida)
        {
            throw new ReglaNegocioException(CodigosEstado.Status401Unauthorized, MensajeCredenciales);
        }

        List<string> roles = await RolesDeUsuario.ObtenerCodigosAsync(_unidad, usuario.Id);
        TokenEmitido token = _tokens.Emitir(usuario.Id, usuario.NombreUsuario, roles);

        return new SesionDto
        {
            Token = token.Token,
            ExpiraEn = token.ExpiraEn,
            Usuario = new UsuarioSesionDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto
            },
            Roles = roles
        };
    }
}
