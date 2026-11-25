using System.Globalization;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

internal static class ValidacionInspeccionInsumo
{
    public const string Pendiente = "Pendiente";
    public const string Aprobada = "Aprobada";
    public const string Rechazada = "Rechazada";
    public const string Anulada = "Anulada";
    private const decimal MaximaCantidadRecibida = 1000000000m;

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

    public static void ValidarIdProducto(int productoId)
    {
        if (productoId <= 0)
        {
            throw Invalida("El identificador del producto debe ser mayor que 0.");
        }
    }

    public static void ValidarIdProveedor(int proveedorId)
    {
        if (proveedorId <= 0)
        {
            throw Invalida("El identificador del proveedor debe ser mayor que 0.");
        }
    }

    public static string NormalizarEstado(string estado)
    {
        string[] estados = { Pendiente, Aprobada, Rechazada, Anulada };
        string? encontrado = estados.FirstOrDefault(valor => valor.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
        if (encontrado == null)
        {
            throw Invalida("El estado de la inspección de insumos no es válido.");
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

    public static string NormalizarLote(string? lote)
    {
        string valor = lote?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Invalida("El lote es obligatorio.");
        }

        if (valor.Length > 50)
        {
            throw Invalida("El lote no puede superar 50 caracteres.");
        }

        return valor.ToUpperInvariant();
    }

    public static DateOnly ValidarFechaInspeccion(DateOnly? fecha)
    {
        if (!fecha.HasValue)
        {
            throw Invalida("La fecha de inspección es obligatoria.");
        }

        if (fecha.Value > FechaPeru.Hoy())
        {
            throw Invalida("La fecha de inspección no puede ser posterior a hoy.");
        }

        return fecha.Value;
    }

    public static decimal ValidarCantidadRecibida(decimal cantidad)
    {
        if (cantidad <= 0 || cantidad > MaximaCantidadRecibida)
        {
            throw Invalida("La cantidad recibida debe ser mayor que 0 y como máximo 1000000000.");
        }

        if (decimal.Round(cantidad, 3) != cantidad)
        {
            throw Invalida("La cantidad recibida admite como máximo 3 decimales.");
        }

        return cantidad;
    }

    public static decimal ValidarCantidadMuestra(decimal muestra, decimal recibida)
    {
        if (muestra <= 0)
        {
            throw Invalida("La cantidad de la muestra debe ser mayor que 0.");
        }

        if (decimal.Round(muestra, 3) != muestra)
        {
            throw Invalida("La cantidad de la muestra admite como máximo 3 decimales.");
        }

        if (muestra > recibida)
        {
            throw Invalida("La muestra no puede superar la cantidad recibida.");
        }

        return muestra;
    }

    public static string NormalizarUnidad(string? unidad)
    {
        string valor = unidad?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Invalida("La unidad de medida es obligatoria.");
        }

        if (valor.Length > 20)
        {
            throw Invalida("La unidad de medida no puede superar 20 caracteres.");
        }

        return valor;
    }

    public static decimal ValidarTolerancia(decimal tolerancia)
    {
        if (tolerancia < 0 || tolerancia > 100)
        {
            throw Invalida("La tolerancia de defectos debe estar entre 0 y 100.");
        }

        if (decimal.Round(tolerancia, 2) != tolerancia)
        {
            throw Invalida("La tolerancia de defectos admite como máximo 2 decimales.");
        }

        return tolerancia;
    }

    public static string NormalizarInspector(string? inspector)
    {
        string valor = inspector?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Invalida("El inspector es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw Invalida("El inspector no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string NormalizarResultado(string? resultado)
    {
        string valor = resultado?.Trim() ?? string.Empty;
        if (valor.Equals(Aprobada, StringComparison.OrdinalIgnoreCase))
        {
            return Aprobada;
        }

        if (valor.Equals(Rechazada, StringComparison.OrdinalIgnoreCase))
        {
            return Rechazada;
        }

        throw Invalida("El resultado debe ser Aprobada o Rechazada.");
    }

    public static decimal ValidarCantidadDefectuosa(decimal defectuosa, decimal muestra)
    {
        if (defectuosa < 0)
        {
            throw Invalida("La cantidad defectuosa no puede ser negativa.");
        }

        if (decimal.Round(defectuosa, 3) != defectuosa)
        {
            throw Invalida("La cantidad defectuosa admite como máximo 3 decimales.");
        }

        if (defectuosa > muestra)
        {
            throw Invalida("La cantidad defectuosa no puede superar la muestra.");
        }

        return defectuosa;
    }

    public static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (valor.Length > 500)
        {
            throw Invalida("Las observaciones no pueden superar 500 caracteres.");
        }

        return valor.Length == 0 ? null : valor;
    }

    public static string NormalizarMotivo(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw Invalida("El motivo de anulación es obligatorio.");
        }

        if (valor.Length > 250)
        {
            throw Invalida("El motivo de anulación no puede superar 250 caracteres.");
        }

        return valor;
    }

    // Porcentaje de defectos de la muestra con 2 decimales; el mismo redondeo en InMemory y en PostgreSQL.
    public static decimal? Porcentaje(decimal? defectuosa, decimal muestra)
    {
        if (!defectuosa.HasValue)
        {
            return null;
        }

        return decimal.Round(defectuosa.Value / muestra * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static bool? SuperaTolerancia(decimal? porcentaje, decimal tolerancia)
    {
        if (!porcentaje.HasValue)
        {
            return null;
        }

        return porcentaje.Value > tolerancia;
    }

    public static decimal TasaRechazo(int aprobadas, int rechazadas)
    {
        int dictaminadas = aprobadas + rechazadas;
        if (dictaminadas == 0)
        {
            return 0m;
        }

        return decimal.Round((decimal)rechazadas / dictaminadas * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static string NuevoNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"CCI-{id:D6}";
    }

    public static ReglaNegocioException NoEncontrada(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La inspección de insumos {id} no existe.");
    }

    public static ReglaNegocioException ProductoInexistente(int id)
    {
        return Invalida($"El producto {id} no existe.");
    }

    public static ReglaNegocioException ProveedorInexistente(int id)
    {
        return Invalida($"El proveedor {id} no existe.");
    }

    public static ReglaNegocioException ProveedorInactivo(int id)
    {
        return Conflicto($"El proveedor {id} está inactivo.");
    }

    public static ReglaNegocioException LoteRepetido(string lote)
    {
        return Conflicto($"Ya existe una inspección del lote {lote} para ese producto y proveedor.");
    }

    public static ReglaNegocioException ToleranciaSuperada(decimal porcentaje, decimal tolerancia)
    {
        string textoPorcentaje = porcentaje.ToString("0.00", CultureInfo.InvariantCulture);
        string textoTolerancia = tolerancia.ToString("0.00", CultureInfo.InvariantCulture);
        return Conflicto(
            $"La inspección supera la tolerancia de defectos ({textoPorcentaje} % > {textoTolerancia} %) y solo puede rechazarse.");
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
