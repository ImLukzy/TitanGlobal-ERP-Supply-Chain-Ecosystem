using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

internal static class ValidacionKardex
{
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

    public static Almacen ValidarAlmacenExistente(Almacen? almacen, int almacenId)
    {
        if (almacen is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El almacén {almacenId} no existe.");
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

    public static string NormalizarTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        string[] tiposValidos = { "Entrada", "Salida" };
        foreach (string tipoValido in tiposValidos)
        {
            if (valor.Equals(tipoValido, StringComparison.OrdinalIgnoreCase))
            {
                return tipoValido;
            }
        }

        throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tipo de movimiento no es válido.");
    }

    public static int ValidarCantidad(int cantidad)
    {
        if (cantidad < 1 || cantidad > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La cantidad del movimiento debe estar entre 1 y 1000000.");
        }

        return cantidad;
    }

    public static decimal? ValidarCostoUnitario(string tipo, decimal? costoUnitario)
    {
        if (tipo == "Salida")
        {
            if (costoUnitario.HasValue)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "Las salidas no llevan costo unitario.");
            }

            return null;
        }

        if (!costoUnitario.HasValue)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El costo unitario es obligatorio en las entradas.");
        }

        decimal costo = costoUnitario.Value;
        if (costo <= 0m || costo > 1000000m || decimal.Round(costo, 4) != costo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El costo unitario debe ser mayor que 0, no superar 1000000 y tener hasta 4 decimales.");
        }

        return costo;
    }

    public static DateTime NormalizarFecha(DateTime? fecha)
    {
        if (!fecha.HasValue)
        {
            return DateTime.UtcNow;
        }

        DateTime valor = FechasUtc.Normalizar(fecha.Value);
        if (valor > DateTime.UtcNow)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha del movimiento no puede ser futura.");
        }

        return valor;
    }

    public static string? NormalizarReferencia(string? referencia)
    {
        string valor = referencia?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La referencia no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string NormalizarMetodo(string? metodo)
    {
        string valor = metodo?.Trim() ?? string.Empty;
        if (valor.Length == 0 || valor.Equals("Promedio", StringComparison.OrdinalIgnoreCase))
        {
            return "Promedio";
        }

        if (valor.Equals("PEPS", StringComparison.OrdinalIgnoreCase))
        {
            return "PEPS";
        }

        throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El método de valoración no es válido.");
    }

    public static ReglaNegocioException CrearStockInsuficiente(long saldo, int cantidad)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Stock insuficiente: el saldo es {saldo} y se pidieron {cantidad}.");
    }
}
