using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

internal static class ValidacionRutaProduccion
{
    public const string Borrador = "Borrador";
    public const string Activa = "Activa";
    public const string Retirada = "Retirada";
    private const decimal MaximoMinutos = 99999999.99m;

    public static void ValidarIdProducto(int productoId)
    {
        if (productoId <= 0)
        {
            throw Invalida("El identificador del producto debe ser mayor que 0.");
        }
    }

    public static string NormalizarEstado(string estado)
    {
        if (estado.Equals(Borrador, StringComparison.OrdinalIgnoreCase))
        {
            return Borrador;
        }

        if (estado.Equals(Activa, StringComparison.OrdinalIgnoreCase))
        {
            return Activa;
        }

        if (estado.Equals(Retirada, StringComparison.OrdinalIgnoreCase))
        {
            return Retirada;
        }

        throw Invalida("El estado de la ruta de producción no es válido.");
    }

    public static async Task<List<OperacionRutaProduccion>> ValidarOperacionesAsync(
        IUnitOfWork unidad, List<OperacionRutaProduccionRequestDto?>? solicitudes)
    {
        if (solicitudes == null || solicitudes.Count < 1 || solicitudes.Count > 100)
        {
            throw Invalida("La ruta de producción debe tener entre 1 y 100 operaciones.");
        }

        List<int> ids = solicitudes.OfType<OperacionRutaProduccionRequestDto>()
            .Select(operacion => operacion.CentroTrabajoId)
            .Distinct()
            .ToList();
        HashSet<int> centros = await ConsultarCentrosAsync(unidad, ids);
        HashSet<int> secuencias = new HashSet<int>();
        List<OperacionRutaProduccion> operaciones = new List<OperacionRutaProduccion>();
        foreach (OperacionRutaProduccionRequestDto? solicitud in solicitudes)
        {
            operaciones.Add(ValidarOperacion(solicitud, secuencias, centros));
        }

        if (secuencias.Max() != operaciones.Count)
        {
            throw Invalida("Las secuencias deben ser consecutivas y empezar en 1.");
        }

        return operaciones;
    }

    private static OperacionRutaProduccion ValidarOperacion(
        OperacionRutaProduccionRequestDto? solicitud, HashSet<int> secuencias, HashSet<int> centros)
    {
        if (solicitud == null)
        {
            throw Invalida("La operación es obligatoria.");
        }

        if (solicitud.Secuencia < 1 || solicitud.Secuencia > 100)
        {
            throw Invalida("La secuencia debe estar entre 1 y 100.");
        }

        if (!secuencias.Add(solicitud.Secuencia))
        {
            throw Invalida($"La secuencia {solicitud.Secuencia} está repetida.");
        }

        string? nombre = solicitud.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre))
        {
            throw Invalida("El nombre de la operación es obligatorio.");
        }

        if (nombre.Length > 120)
        {
            throw Invalida("El nombre de la operación no puede superar 120 caracteres.");
        }

        if (solicitud.CentroTrabajoId <= 0)
        {
            throw Invalida("El identificador del centro de trabajo debe ser mayor que 0.");
        }

        if (!centros.Contains(solicitud.CentroTrabajoId))
        {
            throw CentroInexistente(solicitud.CentroTrabajoId);
        }

        decimal preparacion = solicitud.MinutosPreparacion ?? -1m;
        decimal porUnidad = solicitud.MinutosPorUnidad ?? 0m;
        ValidarTiempos(preparacion, porUnidad);

        return new OperacionRutaProduccion
        {
            Secuencia = solicitud.Secuencia,
            Nombre = nombre,
            CentroTrabajoId = solicitud.CentroTrabajoId,
            MinutosPreparacion = preparacion,
            MinutosPorUnidad = porUnidad
        };
    }

    private static void ValidarTiempos(decimal preparacion, decimal porUnidad)
    {
        if (preparacion < 0 || preparacion > MaximoMinutos)
        {
            throw Invalida("Los minutos de preparación deben estar entre 0 y 99999999.99.");
        }

        if (decimal.Round(preparacion, 2) != preparacion)
        {
            throw Invalida("Los minutos de preparación admiten como máximo 2 decimales.");
        }

        if (porUnidad <= 0 || porUnidad > MaximoMinutos)
        {
            throw Invalida("Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99.");
        }

        if (decimal.Round(porUnidad, 2) != porUnidad)
        {
            throw Invalida("Los minutos por unidad admiten como máximo 2 decimales.");
        }
    }

    public static async Task<HashSet<int>> ConsultarCentrosAsync(IUnitOfWork unidad, List<int> ids)
    {
        List<int> existentes = await unidad.Repository<CentroTrabajo>().AsQueryable()
            .Where(centro => ids.Contains(centro.Id))
            .Select(centro => centro.Id)
            .ToListAsync();
        return existentes.ToHashSet();
    }

    public static ReglaNegocioException CentroInexistente(int id)
    {
        return Invalida($"El centro de trabajo {id} no existe.");
    }

    public static ReglaNegocioException ProductoInexistente(int id)
    {
        return Invalida($"El producto {id} no existe.");
    }

    public static ReglaNegocioException NoEncontrada(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La ruta de producción {id} no existe.");
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
