using System.Text.RegularExpressions;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

internal static class ValidacionAgenteAduanero
{
    internal const int MaximoRazonSocial = 200;

    internal const int MaximoRegistro = 20;

    internal const int MaximoEmail = 150;

    internal const int MaximoTelefono = 20;

    private static readonly Regex FormatoEmail = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal sealed record DatosAgente(
        string Ruc,
        string RazonSocial,
        string NumeroRegistro,
        string? Email,
        string? Telefono);

    internal static DatosAgente Normalizar(
        string? ruc,
        string? razonSocial,
        string? numeroRegistro,
        string? email,
        string? telefono)
    {
        string rucNormalizado = ruc?.Trim() ?? string.Empty;
        if (rucNormalizado.Length != 11 || !rucNormalizado.All(digito => digito >= '0' && digito <= '9'))
        {
            throw Error("El RUC debe tener 11 dígitos.");
        }

        string razon = razonSocial?.Trim() ?? string.Empty;
        if (razon.Length == 0)
        {
            throw Error("La razón social es obligatoria.");
        }

        if (razon.Length > MaximoRazonSocial)
        {
            throw Error($"La razón social no puede superar {MaximoRazonSocial} caracteres.");
        }

        string registro = numeroRegistro?.Trim().ToUpperInvariant() ?? string.Empty;
        if (registro.Length == 0)
        {
            throw Error("El número de registro es obligatorio.");
        }

        if (registro.Length > MaximoRegistro)
        {
            throw Error($"El número de registro no puede superar {MaximoRegistro} caracteres.");
        }

        string? correo = NormalizarOpcional(email);
        if (correo is not null && (correo.Length > MaximoEmail || !FormatoEmail.IsMatch(correo)))
        {
            throw Error("El correo no es válido.");
        }

        string? telefonoNormalizado = NormalizarOpcional(telefono);
        if (telefonoNormalizado is not null && telefonoNormalizado.Length > MaximoTelefono)
        {
            throw Error($"El teléfono no puede superar {MaximoTelefono} caracteres.");
        }

        return new DatosAgente(rucNormalizado, razon, registro, correo, telefonoNormalizado);
    }

    internal static string? NormalizarOpcional(string? texto)
    {
        string valor = texto?.Trim() ?? string.Empty;
        return valor.Length == 0 ? null : valor;
    }

    internal static ReglaNegocioException Error(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    internal static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    internal static ReglaNegocioException NoExiste(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El agente aduanero {id} no existe.");
    }
}
