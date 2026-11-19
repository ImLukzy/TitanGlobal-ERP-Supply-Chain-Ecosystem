using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

internal static class ValidacionAcuerdoAbastecimiento
{
    internal const string Vigente = "Vigente";

    internal const string Suspendido = "Suspendido";

    internal const string Cancelado = "Cancelado";

    internal const int MaximoNumero = 20;

    internal const int MaximoTexto = 500;

    internal const int MaximoCantidad = 1000000;

    internal static readonly string[] Estados = { Vigente, Suspendido, Cancelado };

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

    internal static void ValidarMontoMaximo(decimal monto)
    {
        if (monto <= 0m)
        {
            throw Error("El monto máximo debe ser mayor a 0.");
        }

        bool tieneMasDecimales = decimal.Round(monto, 2) != monto;
        if (tieneMasDecimales || monto >= 10000000000000000m)
        {
            throw Error("El monto máximo no es válido.");
        }
    }

    internal static void ValidarPeriodo(DateTime fechaInicio, DateTime fechaFin, out DateTime inicio, out DateTime fin)
    {
        inicio = FechasUtc.Normalizar(fechaInicio);
        fin = FechasUtc.Normalizar(fechaFin);
        if (fin <= inicio)
        {
            throw Error("La fecha de fin debe ser posterior a la de inicio.");
        }

        if (fin <= DateTime.UtcNow)
        {
            throw Error("La fecha de fin debe ser futura.");
        }
    }

    internal static void ValidarPrecio(decimal precio)
    {
        if (precio <= 0m)
        {
            throw Error("El precio acordado debe ser mayor a 0.");
        }

        bool tieneMasDecimales = decimal.Round(precio, 2) != precio;
        if (tieneMasDecimales || precio >= 10000000000000000m)
        {
            throw Error("El precio acordado no es válido.");
        }
    }

    internal static void ValidarCantidadComprometida(int cantidad)
    {
        if (cantidad <= 0)
        {
            throw Error("La cantidad comprometida debe ser mayor a 0.");
        }

        if (cantidad > MaximoCantidad)
        {
            throw Error($"La cantidad comprometida no puede superar {MaximoCantidad}.");
        }
    }

    internal static string? NormalizarFiltroEstado(string? estado)
    {
        string valor = estado?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        string? canonico = Estados.FirstOrDefault(conocido =>
            string.Equals(conocido, valor, StringComparison.OrdinalIgnoreCase));
        if (canonico is null)
        {
            throw Error("El estado debe ser Vigente, Suspendido o Cancelado.");
        }

        return canonico;
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
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El acuerdo {id} no existe.");
    }

    internal static ReglaNegocioException LineaNoExiste(int id, int lineaId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            $"La línea {lineaId} no existe en el acuerdo {id}.");
    }

    internal static ReglaNegocioException EstaCancelado(int id)
    {
        return Conflicto($"El acuerdo {id} está cancelado.");
    }

    internal static ReglaNegocioException NoVigente(int id)
    {
        return Conflicto($"El acuerdo {id} no está vigente.");
    }
}
