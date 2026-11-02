using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores;

internal static class ValidacionConductor
{
    public static string NormalizarNombre(string? nombre)
    {
        string valor = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El nombre es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El nombre no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string NormalizarDocumentoLicencia(string? documentoLicencia)
    {
        string valor = documentoLicencia?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El documento de licencia es obligatorio.");
        }

        if (valor.Length > 20)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El documento de licencia no puede superar 20 caracteres.");
        }

        return valor;
    }

    public static DateTime NormalizarVencimientoLicencia(DateTime vencimientoLicencia)
    {
        return FechasUtc.Normalizar(vencimientoLicencia);
    }

    public static void ValidarVencimientoLicenciaFutura(DateTime vencimientoLicenciaUtc)
    {
        if (vencimientoLicenciaUtc < DateTime.UtcNow.Date)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de vencimiento de la licencia no es válida.");
        }
    }

    public static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Las observaciones no pueden superar 500 caracteres.");
        }

        return valor;
    }
}
