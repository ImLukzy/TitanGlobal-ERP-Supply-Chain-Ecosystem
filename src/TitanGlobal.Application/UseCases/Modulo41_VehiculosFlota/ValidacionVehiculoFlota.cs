using TitanGlobal.Application.Exceptions;


namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota;

internal static class ValidacionVehiculoFlota
{
    private const decimal CapacidadMaximaKg = 9999999999999999.99m;

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
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La placa es obligatoria.");
        }

        if (valor.Length > 20)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La placa no puede superar 20 caracteres.");
        }

        return valor;
    }

    public static string NormalizarMarca(string? marca)
    {
        string valor = marca?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La marca es obligatoria.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La marca no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string NormalizarModelo(string? modelo)
    {
        string valor = modelo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El modelo es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El modelo no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static int ValidarAnio(int anio)
    {
        int anioMaximo = DateTime.UtcNow.Year + 1;
        if (anio < 1900 || anio > anioMaximo)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El año del vehículo no es válido.");
        }

        return anio;
    }

    public static string NormalizarTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tipo es obligatorio.");
        }

        if (!TiposValidos.Contains(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tipo debe ser Camion, Furgon, Moto u Otro.");
        }

        return TiposValidos.First(t => t.Equals(valor, StringComparison.OrdinalIgnoreCase));
    }

    public static decimal ValidarCapacidadKg(decimal capacidadKg)
    {
        if (capacidadKg <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La capacidad debe ser mayor que cero.");
        }

        if (capacidadKg > CapacidadMaximaKg)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La capacidad no puede superar 9999999999999999.99.");
        }

        if (decimal.Round(capacidadKg, 2) != capacidadKg)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La capacidad no puede tener más de 2 decimales.");
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

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Las observaciones no pueden superar 500 caracteres.");
        }

        return valor;
    }
}
