using System.Text.RegularExpressions;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

internal static class ValidacionLoteSerie
{
    private static readonly Regex FormatoCodigoLote = new Regex(
        @"^[A-Z0-9-]{1,30}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FormatoNumeroSerie = new Regex(
        @"^[A-Z0-9-]{1,50}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Producto ValidarProductoExistente(Producto? producto, int productoId)
    {
        if (producto is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El producto {productoId} no existe.");
        }

        return producto;
    }

    public static string NormalizarCodigoLote(string? codigo)
    {
        string valor = codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El código del lote es obligatorio.");
        }

        if (!FormatoCodigoLote.IsMatch(valor))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código del lote solo admite letras, números y guiones (1 a 30 caracteres).");
        }

        return valor;
    }

    public static int ValidarCantidadLote(int cantidad)
    {
        if (cantidad < 1 || cantidad > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La cantidad del lote debe estar entre 1 y 1000000.");
        }

        return cantidad;
    }

    public static string NormalizarEstadoLote(string? estado)
    {
        string valor = estado?.Trim() ?? string.Empty;
        string[] estadosValidos = { "Disponible", "Cuarentena", "Bloqueado" };
        foreach (string estadoValido in estadosValidos)
        {
            if (valor.Equals(estadoValido, StringComparison.OrdinalIgnoreCase))
            {
                return estadoValido;
            }
        }

        throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado del lote no es válido.");
    }

    public static DateTime? NormalizarFecha(DateTime? fecha)
    {
        if (!fecha.HasValue)
        {
            return null;
        }

        return FechasUtc.Normalizar(DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc));
    }

    public static void ValidarFechas(DateTime? fabricacion, DateTime? vencimiento)
    {
        if (fabricacion.HasValue && vencimiento.HasValue && vencimiento.Value < fabricacion.Value)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de vencimiento no puede ser anterior a la de fabricación.");
        }
    }

    public static ReglaNegocioException CrearConflictoCodigoLote(string codigo, int productoId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe el lote {codigo} para el producto {productoId}.");
    }

    public static ReglaNegocioException CrearLoteNoEncontrado(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El lote {id} no existe.");
    }

    public static string NormalizarNumeroSerie(string? numeroSerie)
    {
        string valor = numeroSerie?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de serie es obligatorio.");
        }

        if (!FormatoNumeroSerie.IsMatch(valor))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El número de serie solo admite letras, números y guiones (1 a 50 caracteres).");
        }

        return valor;
    }

    public static string NormalizarEstadoSerie(string? estado)
    {
        string valor = estado?.Trim() ?? string.Empty;
        string[] estadosValidos = { "Disponible", "Asignada", "Baja" };
        foreach (string estadoValido in estadosValidos)
        {
            if (valor.Equals(estadoValido, StringComparison.OrdinalIgnoreCase))
            {
                return estadoValido;
            }
        }

        throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado de la serie no es válido.");
    }

    public static void ValidarTransicionSerie(string actual, string nuevo)
    {
        bool permitida =
            (actual == "Disponible" && (nuevo == "Asignada" || nuevo == "Baja"))
            || (actual == "Asignada" && (nuevo == "Disponible" || nuevo == "Baja"));
        if (!permitida)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"No se puede pasar la serie de {actual} a {nuevo}.");
        }
    }

    public static ReglaNegocioException CrearConflictoNumeroSerie(string numeroSerie, int productoId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe la serie {numeroSerie} para el producto {productoId}.");
    }

    public static ReglaNegocioException CrearSerieNoEncontrada(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La serie {id} no existe.");
    }
}
