using System.Text.RegularExpressions;
using TitanGlobal.Api.Middlewares;

namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.UseCases;

internal static class ValidacionCentroTrabajo
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
                "El nombre del centro de trabajo es obligatorio.");
        }

        if (valor.Length > 200)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre del centro de trabajo no puede superar 200 caracteres.");
        }

        return valor;
    }

    public static string NormalizarTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        if (valor.Equals("Linea", StringComparison.OrdinalIgnoreCase))
        {
            return "Linea";
        }

        if (valor.Equals("Celda", StringComparison.OrdinalIgnoreCase))
        {
            return "Celda";
        }

        throw new ReglaNegocioException(
            StatusCodes.Status400BadRequest,
            "El tipo de centro de trabajo no es válido.");
    }

    public static string NormalizarPlanta(string? planta)
    {
        string valor = planta?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La planta es obligatoria.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La planta no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static decimal ValidarCapacidad(decimal capacidadHorasDia)
    {
        if (capacidadHorasDia <= 0 || capacidadHorasDia > 24)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La capacidad debe ser mayor que 0 y como máximo 24 horas por día.");
        }

        return capacidadHorasDia;
    }

    public static decimal ValidarCostoHora(decimal costoHora)
    {
        if (costoHora < 0)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El costo por hora no puede ser negativo.");
        }

        return costoHora;
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
