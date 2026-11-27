using MediatR;
using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.Services;
using TitanGlobal.Application.UseCases.Modulo71_Usuarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo00_Autenticacion.Commands;

public class CambiarContrasenaCommand : IRequest
{
    public int UsuarioId { get; set; }

    public CambiarContrasenaRequestDto? Solicitud { get; set; }
}

public class CambiarContrasenaCommandHandler : IRequestHandler<CambiarContrasenaCommand>
{
    private readonly IUnitOfWork _unidad;

    private readonly IServicioContrasenas _contrasenas;

    public CambiarContrasenaCommandHandler(
        IUnitOfWork unidad,
        IServicioContrasenas contrasenas)
    {
        _unidad = unidad;
        _contrasenas = contrasenas;
    }

    public async Task Handle(CambiarContrasenaCommand request, CancellationToken cancellationToken)
    {
        string contrasena = request.Solicitud?.Contrasena ?? string.Empty;
        if (contrasena.Length < LongitudMinima
            || contrasena.Length > LongitudMaxima
            || System.Text.Encoding.UTF8.GetByteCount(contrasena) > LongitudMaxima)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La contraseña debe tener entre 8 y 72 caracteres.");
        }

        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, request.UsuarioId);
        usuario.ContrasenaHash = _contrasenas.Encriptar(contrasena);
        await _unidad.CompleteAsync();
    }

    private const int LongitudMinima = 8;

    // BCrypt solo usa los primeros 72 bytes; se limita por caracteres y por bytes UTF-8.
    private const int LongitudMaxima = 72;
}
