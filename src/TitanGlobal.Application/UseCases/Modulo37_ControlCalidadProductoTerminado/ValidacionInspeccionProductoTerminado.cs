using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

internal static class ValidacionInspeccionProductoTerminado
{
    public const string Pendiente = "Pendiente";
    public const string Aprobada = "Aprobada";
    public const string Rechazada = "Rechazada";
    public const string Anulada = "Anulada";
    public const string OrdenTerminada = "Terminada";

    public static readonly string[] Estados = { Pendiente, Aprobada, Rechazada, Anulada };

    public static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw Invalida("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Invalida("El tamaño de página debe estar entre 1 y 100.");
        }
    }

    public static string? NormalizarBusqueda(string? busqueda)
    {
        string valor = busqueda?.Trim() ?? string.Empty;
        if (valor.Length > 20)
        {
            throw Invalida("La búsqueda no puede superar 20 caracteres.");
        }

        if (valor.Length == 0)
        {
            return null;
        }

        return valor.ToUpperInvariant();
    }

    public static void ValidarIdOrden(int ordenProduccionId)
    {
        if (ordenProduccionId <= 0)
        {
            throw Invalida("El identificador de la orden de producción debe ser mayor que 0.");
        }
    }

    public static void ValidarIdProducto(int productoId)
    {
        if (productoId <= 0)
        {
            throw Invalida("El identificador del producto debe ser mayor que 0.");
        }
    }

    public static string NormalizarEstado(string estado)
    {
        string valor = estado.Trim();
        string? encontrado = Estados.FirstOrDefault(
            existente => existente.Equals(valor, StringComparison.OrdinalIgnoreCase));
        if (encontrado == null)
        {
            throw Invalida("El estado de la inspección de producto terminado no es válido.");
        }

        return encontrado;
    }

    public static void ValidarRangoFechas(DateOnly? desde, DateOnly? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
        {
            throw Invalida("La fecha desde no puede ser posterior a la fecha hasta.");
        }
    }

    public static decimal? PorcentajeNoConforme(int? cantidadNoConforme, int cantidadInspeccionada)
    {
        if (!cantidadNoConforme.HasValue)
        {
            return null;
        }

        return Redondear(cantidadNoConforme.Value * 100m / cantidadInspeccionada);
    }

    public static bool SuperaTolerancia(int cantidadNoConforme, int cantidadInspeccionada, decimal tolerancia)
    {
        // Se compara sin redondear: no conformes / inspeccionadas × 100 > tolerancia.
        return cantidadNoConforme * 100m > tolerancia * cantidadInspeccionada;
    }

    public static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    public static ReglaNegocioException OrdenNoEncontrada(int ordenProduccionId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound, $"La orden de producción {ordenProduccionId} no existe.");
    }

    public static ReglaNegocioException NoEncontrada(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound, $"La inspección de producto terminado {id} no existe.");
    }

    public static ReglaNegocioException Invalida(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }
}
