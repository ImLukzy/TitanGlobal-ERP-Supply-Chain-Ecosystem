using System.Text.RegularExpressions;
using TitanGlobal.Api.Middlewares;

namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.UseCases;

internal static class ValidacionAlmacen
{
    private static readonly Regex FormatoCodigo = new Regex(
        @"^[A-Z0-9-]{2,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string NormalizarCodigo(string? codigo)
    {
        string valor = codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El código es obligatorio.");
        }

        if (!FormatoCodigo.IsMatch(valor))
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El código solo admite letras, números y guiones (2 a 20 caracteres).");
        }

        return valor;
    }

    public static string NormalizarNombre(string? nombre)
    {
        string valor = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre del almacén es obligatorio.");
        }

        if (valor.Length > 200)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre del almacén no puede superar 200 caracteres.");
        }

        return valor;
    }

    public static string NormalizarTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        if (valor.Equals("Central", StringComparison.OrdinalIgnoreCase))
        {
            return "Central";
        }

        if (valor.Equals("Regional", StringComparison.OrdinalIgnoreCase))
        {
            return "Regional";
        }

        if (valor.Equals("Transito", StringComparison.OrdinalIgnoreCase))
        {
            return "Transito";
        }

        throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El tipo de almacén no es válido.");
    }

    public static string NormalizarDireccion(string? direccion)
    {
        string valor = direccion?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La dirección es obligatoria.");
        }

        if (valor.Length > 300)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La dirección no puede superar 300 caracteres.");
        }

        return valor;
    }

    public static string NormalizarCiudad(string? ciudad)
    {
        string valor = ciudad?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La ciudad es obligatoria.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La ciudad no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static decimal ValidarCapacidad(decimal capacidadM3)
    {
        if (capacidadM3 <= 0)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La capacidad debe ser mayor que 0.");
        }

        return capacidadM3;
    }

    public static string? NormalizarResponsable(string? responsable)
    {
        string valor = responsable?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El responsable no puede superar 100 caracteres.");
        }

        return valor;
    }
}
