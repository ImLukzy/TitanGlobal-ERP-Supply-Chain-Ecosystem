using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

internal static class ValidacionDeclaracionAduana
{
    internal const string Registrada = "Registrada";

    internal const string Numerada = "Numerada";

    internal const string Levantada = "Levantada";

    internal const string Anulada = "Anulada";

    internal const int MaximoNumero = 30;

    internal const int MaximoMotivo = 500;

    internal static readonly string[] Estados = { Registrada, Numerada, Levantada, Anulada };

    internal static readonly string[] Regimenes = { "ImportacionConsumo", "AdmisionTemporal", "Deposito" };

    internal static readonly string[] Canales = { "Verde", "Naranja", "Rojo" };

    internal static string NormalizarNumero(string? numero)
    {
        string valor = numero?.Trim().ToUpperInvariant() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Error("El número es obligatorio.");
        }

        if (valor.Length > MaximoNumero)
        {
            throw Error($"El número no puede superar {MaximoNumero} caracteres.");
        }

        return valor;
    }

    internal static string NormalizarRegimen(string? regimen)
    {
        string? canonico = BuscarCanonico(Regimenes, regimen);
        if (canonico is null)
        {
            throw Error("El régimen debe ser ImportacionConsumo, AdmisionTemporal o Deposito.");
        }

        return canonico;
    }

    internal static void ValidarValorAduanero(decimal valor)
    {
        if (valor <= 0)
        {
            throw Error("El valor aduanero debe ser mayor a 0.");
        }

        if (decimal.Round(valor, 2) != valor || valor >= 10_000_000_000_000_000m)
        {
            throw Error("El valor aduanero no es válido.");
        }
    }

    internal static void ValidarTasa(decimal tasa)
    {
        if (tasa < 0 || tasa > 100)
        {
            throw Error("La tasa arancelaria debe estar entre 0 y 100.");
        }

        if (decimal.Round(tasa, 2) != tasa)
        {
            throw Error("La tasa arancelaria no es válida.");
        }
    }

    internal static decimal CalcularDerechos(decimal valorAduanero, decimal tasa)
    {
        return decimal.Round(valorAduanero * tasa / 100m, 2, MidpointRounding.AwayFromZero);
    }

    internal static string NormalizarCanal(string? canal)
    {
        string? canonico = BuscarCanonico(Canales, canal);
        if (canonico is null)
        {
            throw Error("El canal debe ser Verde, Naranja o Rojo.");
        }

        return canonico;
    }

    internal static string? NormalizarFiltroEstado(string? estado)
    {
        string valor = estado?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        string? canonico = BuscarCanonico(Estados, valor);
        if (canonico is null)
        {
            throw Error("El estado debe ser Registrada, Numerada, Levantada o Anulada.");
        }

        return canonico;
    }

    internal static string? NormalizarFiltroCanal(string? canal)
    {
        string valor = canal?.Trim() ?? string.Empty;
        return valor.Length == 0 ? null : NormalizarCanal(valor);
    }

    internal static ReglaNegocioException Error(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    internal static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    internal static ReglaNegocioException NoExiste(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La declaración {id} no existe.");
    }

    internal static ReglaNegocioException NoRegistrada(int id)
    {
        return Conflicto($"La declaración {id} no está registrada.");
    }

    private static string? BuscarCanonico(string[] valores, string? texto)
    {
        string buscado = texto?.Trim() ?? string.Empty;
        return valores.FirstOrDefault(valor =>
            string.Equals(valor, buscado, StringComparison.OrdinalIgnoreCase));
    }
}
