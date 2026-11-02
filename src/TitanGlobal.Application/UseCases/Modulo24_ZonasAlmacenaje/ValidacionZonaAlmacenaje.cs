using System.Globalization;
using System.Text.RegularExpressions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje;

internal static class ValidacionZonaAlmacenaje
{
    private static readonly Regex FormatoCodigo = new Regex(
        @"^[A-Z0-9-]{1,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Almacen ValidarAlmacenExistente(Almacen? almacen, int almacenId)
    {
        if (almacen is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"El almacén {almacenId} no existe.");
        }

        return almacen;
    }

    public static void ValidarAlmacenActivo(Almacen almacen)
    {
        if (!almacen.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El almacén {almacen.Codigo} está inactivo.");
        }
    }

    public static string NormalizarCodigo(string? codigo)
    {
        string valor = codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El código de zona es obligatorio.");
        }

        if (!FormatoCodigo.IsMatch(valor))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código de zona solo admite letras, números y guiones (1 a 20 caracteres).");
        }

        return valor;
    }

    public static string NormalizarNombre(string? nombre)
    {
        string valor = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El nombre de la zona es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El nombre de la zona no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string NormalizarTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        string[] tiposValidos = { "Recepcion", "Almacenamiento", "Picking", "Despacho", "Cuarentena" };
        foreach (string tipoValido in tiposValidos)
        {
            if (valor.Equals(tipoValido, StringComparison.OrdinalIgnoreCase))
            {
                return tipoValido;
            }
        }

        throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tipo de zona no es válido.");
    }

    public static string NormalizarTemperatura(string? temperatura)
    {
        string valor = temperatura?.Trim() ?? string.Empty;
        string[] temperaturasValidas = { "Ambiente", "Refrigerada", "Congelada" };
        foreach (string temperaturaValida in temperaturasValidas)
        {
            if (valor.Equals(temperaturaValida, StringComparison.OrdinalIgnoreCase))
            {
                return temperaturaValida;
            }
        }

        throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La temperatura no es válida.");
    }

    public static decimal ValidarCapacidad(decimal capacidadM3)
    {
        if (capacidadM3 <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La capacidad debe ser mayor que 0.");
        }

        return capacidadM3;
    }

    public static void ValidarCapacidadAsignada(Almacen almacen, decimal capacidadOtrasZonasM3, decimal capacidadZonaM3)
    {
        decimal capacidadTotal = capacidadOtrasZonasM3 + capacidadZonaM3;
        if (capacidadTotal > almacen.CapacidadM3)
        {
            string total = capacidadTotal.ToString("F2", CultureInfo.InvariantCulture);
            string capacidad = almacen.CapacidadM3.ToString("F2", CultureInfo.InvariantCulture);
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La capacidad de las zonas activas ({total} m³) superaría la del almacén {almacen.Codigo} ({capacidad} m³).");
        }
    }

    public static ReglaNegocioException CrearConflictoCodigo(string codigo, string almacenCodigo)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe la zona {codigo} en el almacén {almacenCodigo}.");
    }
}
