using TitanGlobal.Application.Exceptions;


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

    public static DateTime ValidarVencimientoLicencia(DateTime vencimientoLicencia)
    {
        if (vencimientoLicencia < DateTime.UtcNow.Date)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de vencimiento de la licencia no es válida.");
        }

        return vencimientoLicencia;
    }
}
