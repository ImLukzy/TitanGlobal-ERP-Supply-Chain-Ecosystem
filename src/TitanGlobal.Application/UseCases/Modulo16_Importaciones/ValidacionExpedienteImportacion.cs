using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

internal static class ValidacionExpedienteImportacion
{
    internal const string Programado = "Programado";

    internal const string EnTransito = "EnTransito";

    internal const string EnAduana = "EnAduana";

    internal const string Liberado = "Liberado";

    internal const string Cancelado = "Cancelado";

    internal const string Maritimo = "Maritimo";

    internal const string OrdenAnulada = "Anulada";

    internal const int MaximoNumero = 20;

    internal const int MaximoLugar = 100;

    internal const int MaximoDocumento = 40;

    internal const int MaximoTexto = 500;

    internal static readonly string[] Estados = { Programado, EnTransito, EnAduana, Liberado, Cancelado };

    internal static readonly string[] Incoterms =
    {
        "EXW", "FCA", "FAS", "FOB", "CFR", "CIF", "CPT", "CIP", "DAP", "DPU", "DDP"
    };

    internal static readonly string[] IncotermsMaritimos = { "FAS", "FOB", "CFR", "CIF" };

    internal static readonly string[] MediosTransporte = { Maritimo, "Aereo", "Terrestre" };

    internal sealed record DatosExpediente(
        string Incoterm,
        string MedioTransporte,
        string PaisOrigen,
        string LugarOrigen,
        string LugarDestino,
        DateTime FechaLlegadaEstimada,
        string? Observaciones);

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

    /// <summary>Reglas 2 a 7 del alta: las mismas en la edición, en el mismo orden.</summary>
    internal static DatosExpediente NormalizarDatos(
        string? incoterm,
        string? medioTransporte,
        string? paisOrigen,
        string? lugarOrigen,
        string? lugarDestino,
        DateTime fechaLlegadaEstimada,
        string? observaciones)
    {
        string incotermNormalizado = NormalizarIncoterm(incoterm);
        string medioNormalizado = NormalizarMedio(medioTransporte);
        if (IncotermsMaritimos.Contains(incotermNormalizado) && medioNormalizado != Maritimo)
        {
            throw Error($"El Incoterm {incotermNormalizado} solo aplica al transporte marítimo.");
        }

        string pais = NormalizarLugar(paisOrigen, "El país de origen");
        string origen = NormalizarLugar(lugarOrigen, "El lugar de origen");
        string destino = NormalizarLugar(lugarDestino, "El lugar de destino");
        DateTime llegada = FechasUtc.Normalizar(fechaLlegadaEstimada);
        if (llegada <= DateTime.UtcNow)
        {
            throw Error("La fecha de llegada estimada debe ser futura.");
        }

        string? observacionesNormalizadas = NormalizarObservaciones(observaciones);

        return new DatosExpediente(incotermNormalizado, medioNormalizado, pais, origen, destino, llegada, observacionesNormalizadas);
    }

    internal static string NormalizarIncoterm(string? incoterm)
    {
        string? canonico = BuscarCanonico(Incoterms, incoterm);
        if (canonico is null)
        {
            throw Error("El Incoterm no es válido.");
        }

        return canonico;
    }

    internal static string NormalizarMedio(string? medioTransporte)
    {
        string? canonico = BuscarCanonico(MediosTransporte, medioTransporte);
        if (canonico is null)
        {
            throw Error("El medio de transporte debe ser Maritimo, Aereo o Terrestre.");
        }

        return canonico;
    }

    internal static string NormalizarLugar(string? texto, string etiqueta)
    {
        string valor = texto?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Error($"{etiqueta} es obligatorio.");
        }

        if (valor.Length > MaximoLugar)
        {
            throw Error($"{etiqueta} no puede superar {MaximoLugar} caracteres.");
        }

        return valor;
    }

    internal static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > MaximoTexto)
        {
            throw Error($"Las observaciones no pueden superar {MaximoTexto} caracteres.");
        }

        return valor;
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
            throw Error("El estado debe ser Programado, EnTransito, EnAduana, Liberado o Cancelado.");
        }

        return canonico;
    }

    internal static string? NormalizarFiltroIncoterm(string? incoterm)
    {
        string valor = incoterm?.Trim() ?? string.Empty;
        return valor.Length == 0 ? null : NormalizarIncoterm(valor);
    }

    internal static string? NormalizarFiltroMedio(string? medioTransporte)
    {
        string valor = medioTransporte?.Trim() ?? string.Empty;
        return valor.Length == 0 ? null : NormalizarMedio(valor);
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
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El expediente {id} no existe.");
    }

    internal static ReglaNegocioException NoProgramado(int id)
    {
        return Conflicto($"El expediente {id} no está programado.");
    }

    private static string? BuscarCanonico(string[] valores, string? texto)
    {
        string buscado = texto?.Trim() ?? string.Empty;
        return valores.FirstOrDefault(valor =>
            string.Equals(valor, buscado, StringComparison.OrdinalIgnoreCase));
    }
}
