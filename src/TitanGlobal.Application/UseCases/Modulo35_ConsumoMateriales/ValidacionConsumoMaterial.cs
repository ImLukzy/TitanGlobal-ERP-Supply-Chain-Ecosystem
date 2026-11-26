using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

internal static class ValidacionConsumoMaterial
{
    public const string Registrado = "Registrado";
    public const string Anulado = "Anulado";
    public const string OrdenEnProceso = "EnProceso";
    public const decimal MaximaCantidad = 1000000m;
    public const string ReferenciaOrden = "FK_ConsumosMateriales_OrdenesProduccion_OrdenProduccionId";
    public const string ReferenciaComponente = "FK_ConsumosMateriales_Productos_ComponenteProductoId";

    // Perú es UTC−5 y no tiene horario de verano: el día D de Perú va de D 05:00 UTC a D + 1 05:00 UTC.
    private const int DesfasePeruHoras = 5;

    public static readonly string[] Estados = { Registrado, Anulado };

    public static DateOnly HoyEnPeru()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-DesfasePeruHoras));
    }

    public static DateTime InicioDelDiaEnPeru(DateOnly dia)
    {
        return new DateTime(dia.Year, dia.Month, dia.Day, DesfasePeruHoras, 0, 0, DateTimeKind.Utc);
    }

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

    public static void ValidarIdComponente(int componenteProductoId)
    {
        if (componenteProductoId <= 0)
        {
            throw Invalida("El identificador del componente debe ser mayor que 0.");
        }
    }

    public static string NormalizarEstado(string estado)
    {
        string valor = estado.Trim();
        string? encontrado = Estados.FirstOrDefault(
            existente => existente.Equals(valor, StringComparison.OrdinalIgnoreCase));
        if (encontrado == null)
        {
            throw Invalida("El estado del consumo de materiales no es válido.");
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

    public static decimal ValidarCantidad(decimal? cantidad)
    {
        decimal valor = cantidad ?? 0m;
        if (valor <= 0m || valor > MaximaCantidad)
        {
            throw Invalida("La cantidad consumida debe ser mayor que 0 y como máximo 1000000.");
        }

        if (decimal.Round(valor, 6) != valor)
        {
            throw Invalida("La cantidad consumida admite como máximo 6 decimales.");
        }

        return valor;
    }

    public static string? NormalizarTextoOpcional(string? texto, int maximo, string mensaje)
    {
        string? valor = texto?.Trim();
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        if (valor.Length > maximo)
        {
            throw Invalida(mensaje);
        }

        return valor;
    }

    public static string NumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"CM-{id:D6}";
    }

    public static ReglaNegocioException OrdenInexistenteEnRegistro(int ordenProduccionId)
    {
        return Invalida($"La orden de producción {ordenProduccionId} no existe.");
    }

    public static ReglaNegocioException ComponenteFueraDeLista(int componenteProductoId, string numeroOrden)
    {
        return Conflicto(
            $"El producto {componenteProductoId} no forma parte de la lista de materiales de la orden {numeroOrden}.");
    }

    public static ReglaNegocioException OrdenNoEncontrada(int ordenProduccionId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound, $"La orden de producción {ordenProduccionId} no existe.");
    }

    public static ReglaNegocioException NoEncontrado(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El consumo de materiales {id} no existe.");
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
