using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones;

public static class ValidacionCotizacion
{
    public static async Task<CotizacionNormalizada> ValidarAsync(
        GuardarCotizacionRequestDto solicitud,
        IUnitOfWork unitOfWork)
    {
        bool clienteExiste = await unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .AnyAsync(cliente => cliente.Id == solicitud.ClienteId);
        if (!clienteExiste)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El cliente {solicitud.ClienteId} no existe.");
        }

        DateTime fechaValidez = FechasUtc.Normalizar(solicitud.FechaValidez);
        DateTime hoyUtc = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        if (fechaValidez.Date < hoyUtc)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de validez no puede ser anterior a hoy.");
        }

        string observacionesSinEspacios = solicitud.Observaciones?.Trim() ?? string.Empty;
        string? observaciones = string.IsNullOrWhiteSpace(observacionesSinEspacios)
            ? null
            : observacionesSinEspacios;
        if (observaciones is not null && observaciones.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Las observaciones no pueden superar 500 caracteres.");
        }

        List<LineaCotizacionRequestDto>? lineasSolicitud = solicitud.Lineas;
        if (lineasSolicitud is null || lineasSolicitud.Count == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La cotización debe tener al menos una línea.");
        }

        HashSet<int> productosUnicos = new HashSet<int>();
        foreach (LineaCotizacionRequestDto linea in lineasSolicitud)
        {
            if (linea.Cantidad <= 0)
            {
                throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La cantidad debe ser mayor que 0.");
            }

            if (linea.PrecioUnitario <= 0)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "El precio unitario debe ser mayor que 0.");
            }

            if (linea.DescuentoPorcentaje < 0 || linea.DescuentoPorcentaje > 100)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "El descuento debe estar entre 0 y 100.");
            }

            if (!productosUnicos.Add(linea.ProductoId))
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    $"El producto {linea.ProductoId} está repetido en la cotización.");
            }
        }

        List<int> productosSolicitados = lineasSolicitud.Select(linea => linea.ProductoId).ToList();
        List<int> productosExistentes = await unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => productosSolicitados.Contains(producto.Id))
            .Select(producto => producto.Id)
            .ToListAsync();
        HashSet<int> productosExistentesSet = productosExistentes.ToHashSet();
        foreach (LineaCotizacionRequestDto linea in lineasSolicitud)
        {
            if (!productosExistentesSet.Contains(linea.ProductoId))
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    $"El producto {linea.ProductoId} no existe.");
            }
        }

        List<LineaCotizacionRequestDto> lineasNormalizadas = lineasSolicitud
            .Select(linea => new LineaCotizacionRequestDto
            {
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = decimal.Round(linea.PrecioUnitario, 2, MidpointRounding.AwayFromZero),
                DescuentoPorcentaje = decimal.Round(linea.DescuentoPorcentaje, 2, MidpointRounding.AwayFromZero)
            })
            .ToList();
        return new CotizacionNormalizada(fechaValidez, observaciones, lineasNormalizadas);
    }

    public static string? NormalizarMotivo(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El motivo de rechazo es obligatorio.");
        }

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de rechazo no puede superar 500 caracteres.");
        }

        return valor;
    }

    public static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }
    }
}

public sealed record CotizacionNormalizada(
    DateTime FechaValidez,
    string? Observaciones,
    List<LineaCotizacionRequestDto> Lineas);
