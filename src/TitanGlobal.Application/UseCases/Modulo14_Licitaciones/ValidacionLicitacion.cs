using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

internal static class ValidacionLicitacion
{
    internal const string Abierta = "Abierta";

    internal const string Cerrada = "Cerrada";

    internal const string Adjudicada = "Adjudicada";

    internal const string Cancelada = "Cancelada";

    internal const string ConcursoPrecios = "ConcursoPrecios";

    internal const string SubastaInversa = "SubastaInversa";

    internal const int MaximoNumero = 20;

    internal const int MaximoTitulo = 150;

    internal const int MaximoTexto = 500;

    internal static readonly string[] Estados = { Abierta, Cerrada, Adjudicada, Cancelada };

    internal static readonly string[] Tipos = { ConcursoPrecios, SubastaInversa };

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

    internal static string NormalizarTitulo(string? titulo)
    {
        string valor = titulo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Error("El título es obligatorio.");
        }

        if (valor.Length > MaximoTitulo)
        {
            throw Error($"El título no puede superar {MaximoTitulo} caracteres.");
        }

        return valor;
    }

    internal static string NormalizarTipo(string? tipo)
    {
        string? canonico = BuscarCanonico(Tipos, tipo);
        if (canonico is null)
        {
            throw Error("El tipo debe ser ConcursoPrecios o SubastaInversa.");
        }

        return canonico;
    }

    internal static void ValidarPresupuesto(decimal presupuesto)
    {
        if (presupuesto <= 0m)
        {
            throw Error("El presupuesto referencial debe ser mayor a 0.");
        }

        bool tieneMasDecimales = decimal.Round(presupuesto, 2) != presupuesto;
        if (tieneMasDecimales || presupuesto >= 10000000000000000m)
        {
            throw Error("El presupuesto referencial no es válido.");
        }
    }

    internal static DateTime ValidarFechaCierre(DateTime fechaCierre)
    {
        DateTime fecha = FechasUtc.Normalizar(fechaCierre);
        if (fecha <= DateTime.UtcNow)
        {
            throw Error("La fecha de cierre debe ser futura.");
        }

        return fecha;
    }

    internal static string? NormalizarDescripcion(string? descripcion)
    {
        string valor = descripcion?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > MaximoTexto)
        {
            throw Error($"La descripción no puede superar {MaximoTexto} caracteres.");
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

    internal static void ValidarMonto(decimal monto)
    {
        if (monto <= 0m)
        {
            throw Error("El monto debe ser mayor a 0.");
        }
    }

    internal static void ValidarPlazo(int plazoEntregaDias)
    {
        if (plazoEntregaDias < 1 || plazoEntregaDias > 365)
        {
            throw Error("El plazo de entrega debe estar entre 1 y 365 días.");
        }
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
            throw Error("El estado debe ser Abierta, Cerrada, Adjudicada o Cancelada.");
        }

        return canonico;
    }

    internal static string? NormalizarFiltroTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        return NormalizarTipo(valor);
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
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La licitación {id} no existe.");
    }

    internal static ReglaNegocioException NoAbierta(int id)
    {
        return Conflicto($"La licitación {id} no está abierta.");
    }

    internal static ReglaNegocioException PlazoVencido(int id)
    {
        return Conflicto($"La licitación {id} ya venció su plazo de ofertas.");
    }

    private static string? BuscarCanonico(string[] valores, string? texto)
    {
        string buscado = texto?.Trim() ?? string.Empty;
        return valores.FirstOrDefault(valor =>
            string.Equals(valor, buscado, StringComparison.OrdinalIgnoreCase));
    }
}
