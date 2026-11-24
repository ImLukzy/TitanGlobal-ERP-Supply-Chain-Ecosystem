using System.Globalization;
using System.Text.RegularExpressions;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

internal static class ValidacionEvaluacionProveedor
{
    internal const int MaximoObservaciones = 500;

    internal static readonly string[] Calificaciones = { "Excelente", "Bueno", "Regular", "Deficiente" };

    private static readonly Regex FormatoPeriodo = new Regex(
        @"^(?<anio>[0-9]{4})-(?<mes>0[1-9]|1[0-2])$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static void ValidarProveedor(int proveedorId)
    {
        if (proveedorId <= 0)
        {
            throw Error("El proveedor es obligatorio.");
        }
    }

    internal static string NormalizarPeriodo(string? periodo)
    {
        string valor = periodo?.Trim() ?? string.Empty;
        Match coincidencia = FormatoPeriodo.Match(valor);
        if (!coincidencia.Success)
        {
            throw Error("El periodo debe tener el formato AAAA-MM.");
        }

        int anio = int.Parse(coincidencia.Groups["anio"].Value, CultureInfo.InvariantCulture);
        if (anio < 2000 || anio > 2100)
        {
            throw Error("El periodo debe tener el formato AAAA-MM.");
        }

        return valor;
    }

    internal static void ValidarPeriodoNoFuturo(string periodo)
    {
        string actual = DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        if (string.CompareOrdinal(periodo, actual) > 0)
        {
            throw Error("El periodo no puede ser futuro.");
        }
    }

    internal static void ValidarPuntajes(int cumplimiento, int calidad, int plazos)
    {
        if (cumplimiento < 0 || cumplimiento > 100)
        {
            throw Error("El puntaje de cumplimiento debe estar entre 0 y 100.");
        }

        if (calidad < 0 || calidad > 100)
        {
            throw Error("El puntaje de calidad debe estar entre 0 y 100.");
        }

        if (plazos < 0 || plazos > 100)
        {
            throw Error("El puntaje de plazos debe estar entre 0 y 100.");
        }
    }

    internal static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > MaximoObservaciones)
        {
            throw Error($"Las observaciones no pueden superar {MaximoObservaciones} caracteres.");
        }

        return valor;
    }

    internal static decimal CalcularPuntajeFinal(int cumplimiento, int calidad, int plazos)
    {
        decimal final = (cumplimiento * 0.40m) + (calidad * 0.30m) + (plazos * 0.30m);
        return Math.Round(final, 2, MidpointRounding.AwayFromZero);
    }

    internal static string CalcularCalificacion(decimal puntajeFinal)
    {
        if (puntajeFinal >= 90m)
        {
            return "Excelente";
        }

        if (puntajeFinal >= 75m)
        {
            return "Bueno";
        }

        return puntajeFinal >= 60m ? "Regular" : "Deficiente";
    }

    internal static string? NormalizarFiltroCalificacion(string? calificacion)
    {
        string valor = calificacion?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        string? canonica = Calificaciones.FirstOrDefault(nombre =>
            string.Equals(nombre, valor, StringComparison.OrdinalIgnoreCase));
        if (canonica is null)
        {
            throw Error("La calificación debe ser Excelente, Bueno, Regular o Deficiente.");
        }

        return canonica;
    }

    private static ReglaNegocioException Error(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }
}
