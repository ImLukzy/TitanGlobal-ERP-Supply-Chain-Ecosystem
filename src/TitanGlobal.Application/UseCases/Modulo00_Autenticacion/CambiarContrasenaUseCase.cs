using TitanGlobal.Application.DTOs.Modulo00_Autenticacion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.Services;
using TitanGlobal.Application.UseCases.Modulo71_Usuarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo00_Autenticacion;

public class CambiarContrasenaUseCase
{
    private const int LongitudMinima = 8;

    // BCrypt solo usa los primeros 72 bytes; se limita por caracteres y por bytes UTF-8.
    private const int LongitudMaxima = 72;

    private readonly IUnitOfWork _unidad;
    private readonly IServicioContrasenas _contrasenas;

    public CambiarContrasenaUseCase(IUnitOfWork unidad, IServicioContrasenas contrasenas)
    {
        _unidad = unidad;
        _contrasenas = contrasenas;
    }

    public async Task ExecuteAsync(int usuarioId, CambiarContrasenaRequestDto? solicitud)
    {
        string contrasena = solicitud?.Contrasena ?? string.Empty;
        if (contrasena.Length < LongitudMinima
            || contrasena.Length > LongitudMaxima
            || System.Text.Encoding.UTF8.GetByteCount(contrasena) > LongitudMaxima)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La contraseña debe tener entre 8 y 72 caracteres.");
        }

        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, usuarioId);
        usuario.ContrasenaHash = _contrasenas.Encriptar(contrasena);
        await _unidad.CompleteAsync();
    }
}
