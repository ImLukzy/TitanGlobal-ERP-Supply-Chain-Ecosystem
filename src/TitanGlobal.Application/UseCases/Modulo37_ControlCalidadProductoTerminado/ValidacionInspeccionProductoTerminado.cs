using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

internal sealed record DatosInspeccion(
    DateOnly FechaInspeccion, int CantidadInspeccionada, decimal Tolerancia, string Inspector);

internal static class ValidacionInspeccionProductoTerminado
{
    public const int MaximoUnidades = 1000000;
    public const string ReferenciaOrden = "FK_InspeccionesProductoTerminado_OrdenesProduccion_OrdenProduccionId";

    // Perú es UTC−5 y no tiene horario de verano.
    private const int DesfasePeruHoras = 5;

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

    public static DateOnly HoyEnPeru()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-DesfasePeruHoras));
    }

    public static DatosInspeccion ValidarDatos(
        DateOnly? fechaInspeccion, int cantidadInspeccionada, decimal? tolerancia, string? inspector)
    {
        if (!fechaInspeccion.HasValue)
        {
            throw Invalida("La fecha de inspección es obligatoria.");
        }

        if (fechaInspeccion.Value > HoyEnPeru())
        {
            throw Invalida("La fecha de inspección no puede ser posterior a hoy.");
        }

        if (cantidadInspeccionada < 1 || cantidadInspeccionada > MaximoUnidades)
        {
            throw Invalida("La cantidad inspeccionada debe estar entre 1 y 1000000.");
        }

        decimal valorTolerancia = tolerancia ?? -1m;
        if (valorTolerancia < 0m || valorTolerancia > 100m)
        {
            throw Invalida("La tolerancia de no conformes debe estar entre 0 y 100.");
        }

        if (decimal.Round(valorTolerancia, 2) != valorTolerancia)
        {
            throw Invalida("La tolerancia de no conformes admite como máximo 2 decimales.");
        }

        string nombre = inspector?.Trim() ?? string.Empty;
        if (nombre.Length == 0)
        {
            throw Invalida("El inspector es obligatorio.");
        }

        if (nombre.Length > 100)
        {
            throw Invalida("El inspector no puede superar 100 caracteres.");
        }

        return new DatosInspeccion(fechaInspeccion.Value, cantidadInspeccionada, valorTolerancia, nombre);
    }

    public static async Task<OrdenProduccion> ValidarOrdenAsync(
        IUnitOfWork unidad, int ordenProduccionId, DatosInspeccion datos, int? inspeccionExcluida)
    {
        OrdenProduccion? orden = await unidad.Repository<OrdenProduccion>().AsQueryable()
            .SingleOrDefaultAsync(existente => existente.Id == ordenProduccionId);
        if (orden == null)
        {
            throw Invalida($"La orden de producción {ordenProduccionId} no existe.");
        }

        if (orden.Estado != OrdenTerminada)
        {
            throw Conflicto("Solo se pueden inspeccionar órdenes de producción terminadas.");
        }

        if (orden.FechaFinReal.HasValue
            && datos.FechaInspeccion < DateOnly.FromDateTime(orden.FechaFinReal.Value.AddHours(-DesfasePeruHoras)))
        {
            throw Invalida($"La fecha de inspección no puede ser anterior al fin de la orden {orden.Numero}.");
        }

        // PATRÓN LINQ 4: suma de las unidades ya asignadas a inspecciones no anuladas de la orden.
        int asignadas = await unidad.Repository<InspeccionProductoTerminado>().AsQueryable()
            .Where(inspeccion => inspeccion.OrdenProduccionId == ordenProduccionId
                && inspeccion.Estado != Anulada
                && (!inspeccionExcluida.HasValue || inspeccion.Id != inspeccionExcluida.Value))
            .SumAsync(inspeccion => inspeccion.CantidadInspeccionada);
        int disponibles = Math.Max((orden.CantidadProducida ?? 0) - asignadas, 0);
        if (datos.CantidadInspeccionada > disponibles)
        {
            throw Conflicto($"La orden {orden.Numero} solo tiene {disponibles} unidades por inspeccionar.");
        }

        return orden;
    }

    public static async Task<InspeccionProductoTerminado> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        InspeccionProductoTerminado? inspeccion = await unidad.Repository<InspeccionProductoTerminado>()
            .AsQueryable()
            .SingleOrDefaultAsync(existente => existente.Id == id);
        return inspeccion ?? throw NoEncontrada(id);
    }

    public static string NumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"CCP-{id:D6}";
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
