using TitanGlobal.Api.Middlewares;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

internal static class ValidacionVehiculoFlota
{
    private static readonly HashSet<string> TiposValidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Camion",
        "Furgon",
        "Moto",
        "Otro"
    };

    public static string NormalizarPlaca(string? placa)
    {
        string valor = placa?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La placa es obligatoria.");
        }

        if (valor.Length > 20)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La placa no puede superar 20 caracteres.");
        }

        return valor;
    }

    public static string NormalizarMarca(string? marca)
    {
        string valor = marca?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La marca es obligatoria.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La marca no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string NormalizarModelo(string? modelo)
    {
        string valor = modelo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El modelo es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El modelo no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static int ValidarAnio(int anio)
    {
        int anioMaximo = DateTime.UtcNow.Year + 1;
        if (anio < 1900 || anio > anioMaximo)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El año del vehículo no es válido.");
        }

        return anio;
    }

    public static string NormalizarTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El tipo es obligatorio.");
        }

        if (!TiposValidos.Contains(valor))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El tipo debe ser Camion, Furgon, Moto u Otro.");
        }

        return TiposValidos.First(t => t.Equals(valor, StringComparison.OrdinalIgnoreCase));
    }

    public static decimal ValidarCapacidadKg(decimal capacidadKg)
    {
        if (capacidadKg <= 0)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La capacidad debe ser mayor que cero.");
        }

        return capacidadKg;
    }

    public static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        return valor;
    }
}
