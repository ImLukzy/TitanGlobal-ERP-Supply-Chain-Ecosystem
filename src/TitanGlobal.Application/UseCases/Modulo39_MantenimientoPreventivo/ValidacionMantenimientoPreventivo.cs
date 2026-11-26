using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

internal static class ValidacionMantenimientoPreventivo
{
    public const string Programado = "Programado";
    public const string EnEjecucion = "EnEjecucion";
    public const string Completado = "Completado";
    public const string Cancelado = "Cancelado";
    private const decimal MaximoHoras = 168m;

    public static DateOnly Hoy()
    {
        return FechaPeru.Hoy();
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
        if (valor.Length > 50)
        {
            throw Invalida("La búsqueda no puede superar 50 caracteres.");
        }

        return valor.Length == 0 ? null : valor.ToLower();
    }

    public static void ValidarIdCentro(int centroTrabajoId)
    {
        if (centroTrabajoId <= 0)
        {
            throw Invalida("El identificador del centro de trabajo debe ser mayor que 0.");
        }
    }

    public static string NormalizarEstado(string estado)
    {
        string[] estados = { Programado, EnEjecucion, Completado, Cancelado };
        string? encontrado = estados.FirstOrDefault(valor => valor.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
        if (encontrado == null)
        {
            throw Invalida("El estado del mantenimiento preventivo no es válido.");
        }

        return encontrado;
    }

    public static void ValidarRango(DateOnly? desde, DateOnly? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
        {
            throw Invalida("La fecha desde no puede ser posterior a la fecha hasta.");
        }
    }

    public static string NormalizarDescripcion(string? descripcion)
    {
        string valor = descripcion?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Invalida("La descripción del mantenimiento es obligatoria.");
        }

        if (valor.Length > 250)
        {
            throw Invalida("La descripción del mantenimiento no puede superar 250 caracteres.");
        }

        return valor;
    }

    public static DateOnly ValidarFechaProgramada(DateOnly? fecha)
    {
        if (!fecha.HasValue)
        {
            throw Invalida("La fecha programada es obligatoria.");
        }

        if (fecha.Value < Hoy())
        {
            throw Invalida("La fecha programada no puede ser anterior a hoy.");
        }

        return fecha.Value;
    }

    public static decimal ValidarDuracion(decimal duracion)
    {
        if (duracion <= 0 || duracion > MaximoHoras)
        {
            throw Invalida("La duración debe ser mayor que 0 y como máximo 168 horas.");
        }

        if (decimal.Round(duracion, 2) != duracion)
        {
            throw Invalida("La duración admite como máximo 2 decimales.");
        }

        return duracion;
    }

    public static int? ValidarFrecuencia(int? frecuenciaDias)
    {
        if (frecuenciaDias.HasValue && (frecuenciaDias.Value < 1 || frecuenciaDias.Value > 3650))
        {
            throw Invalida("La frecuencia debe estar entre 1 y 3650 días.");
        }

        return frecuenciaDias;
    }

    public static string? NormalizarResponsable(string? responsable)
    {
        return NormalizarOpcional(responsable, 100, "El responsable no puede superar 100 caracteres.");
    }

    public static decimal ValidarHorasReales(decimal horas)
    {
        if (horas <= 0 || horas > MaximoHoras)
        {
            throw Invalida("Las horas reales deben ser mayores que 0 y como máximo 168.");
        }

        if (decimal.Round(horas, 2) != horas)
        {
            throw Invalida("Las horas reales admiten como máximo 2 decimales.");
        }

        return horas;
    }

    public static string? NormalizarObservaciones(string? observaciones)
    {
        return NormalizarOpcional(observaciones, 500, "Las observaciones no pueden superar 500 caracteres.");
    }

    public static string NormalizarMotivo(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Invalida("El motivo de cancelación es obligatorio.");
        }

        if (valor.Length > 250)
        {
            throw Invalida("El motivo de cancelación no puede superar 250 caracteres.");
        }

        return valor;
    }

    public static DateOnly? CalcularSiguiente(string estado, DateTime? fechaFinReal, int? frecuenciaDias)
    {
        if (estado != Completado || !fechaFinReal.HasValue || !frecuenciaDias.HasValue)
        {
            return null;
        }

        return DateOnly.FromDateTime(fechaFinReal.Value).AddDays(frecuenciaDias.Value);
    }

    public static string NuevoNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"MP-{id:D6}";
    }

    public static ReglaNegocioException NoEncontrado(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound, $"El mantenimiento preventivo {id} no existe.");
    }

    public static ReglaNegocioException CentroInexistente(int id)
    {
        return Invalida($"El centro de trabajo {id} no existe.");
    }

    public static ReglaNegocioException CentroInactivo(int id)
    {
        return Conflicto($"El centro de trabajo {id} está inactivo.");
    }

    public static ReglaNegocioException FechaOcupada(string codigoCentro, DateOnly fecha)
    {
        return Conflicto(
            $"El centro de trabajo {codigoCentro} ya tiene un mantenimiento programado el {fecha:yyyy-MM-dd}.");
    }

    public static ReglaNegocioException Invalida(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    private static string? NormalizarOpcional(string? valor, int maximo, string mensaje)
    {
        string texto = valor?.Trim() ?? string.Empty;
        if (texto.Length > maximo)
        {
            throw Invalida(mensaje);
        }

        return texto.Length == 0 ? null : texto;
    }
}
