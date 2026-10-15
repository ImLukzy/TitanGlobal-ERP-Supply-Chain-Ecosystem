using System.Text.RegularExpressions;
using TitanGlobal.Api.Middlewares;

namespace TitanGlobal.Api.Modules.Modulo11_Proveedores.UseCases;

internal static class ValidacionProveedor
{
    private static readonly Regex FormatoEmail = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FormatoTelefono = new Regex(
        @"^[0-9 +\-]{6,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string NormalizarRazonSocial(string? razonSocial)
    {
        string valor = razonSocial?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La razón social es obligatoria.");
        }

        if (valor.Length > 200)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La razón social no puede superar 200 caracteres.");
        }

        return valor;
    }

    public static string NormalizarRuc(string? ruc)
    {
        string valor = ruc?.Trim() ?? string.Empty;
        if (valor.Length != 11 || !valor.All(digito => digito >= '0' && digito <= '9'))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El RUC debe tener 11 dígitos numéricos.");
        }

        return valor;
    }

    public static string NormalizarPais(string? pais)
    {
        string valor = pais?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El país es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El país no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string? NormalizarEmail(string? email)
    {
        string valor = email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        if (valor.Length > 150 || !FormatoEmail.IsMatch(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El email no tiene un formato válido.");
        }

        return valor;
    }

    public static string? NormalizarTelefono(string? telefono)
    {
        string valor = telefono?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        if (!FormatoTelefono.IsMatch(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El teléfono no tiene un formato válido.");
        }

        return valor;
    }
}
