using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra;

public static class ValidacionSolicitudCompra
{
    public static async Task<GuardarSolicitudRequestDto> ValidarAsync(
        GuardarSolicitudRequestDto solicitud,
        IUnitOfWork unitOfWork)
    {
        solicitud.Solicitante = NormalizarSolicitante(solicitud.Solicitante);
        solicitud.Observaciones = NormalizarObservaciones(solicitud.Observaciones);
        List<LineaSolicitudRequestDto> lineas = ValidarLineas(solicitud.Lineas);
        solicitud.Lineas = lineas;

        List<int> productosSolicitados = lineas.Select(linea => linea.ProductoId).Distinct().ToList();
        List<int> productosExistentes = await unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => productosSolicitados.Contains(producto.Id))
            .Select(producto => producto.Id)
            .ToListAsync();
        HashSet<int> productosExistentesSet = productosExistentes.ToHashSet();
        foreach (int productoId in productosSolicitados)
        {
            if (!productosExistentesSet.Contains(productoId))
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    $"El producto {productoId} no existe.");
            }
        }

        if (solicitud.ProveedorId.HasValue)
        {
            Proveedor? proveedor = await unitOfWork.Repository<Proveedor>()
                .AsQueryable()
                .Where(entidad => entidad.Id == solicitud.ProveedorId.Value)
                .Select(entidad => new Proveedor
                {
                    Id = entidad.Id,
                    Activo = entidad.Activo
                })
                .SingleOrDefaultAsync();
            if (proveedor is null)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    $"El proveedor {solicitud.ProveedorId.Value} no existe.");
            }

            if (!proveedor.Activo)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    $"El proveedor {solicitud.ProveedorId.Value} está inactivo.");
            }
        }

        return solicitud;
    }

    private static string NormalizarSolicitante(string? solicitante)
    {
        string valor = solicitante?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El solicitante es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El solicitante no puede superar 100 caracteres.");
        }

        return valor;
    }

    private static string? NormalizarObservaciones(string? observaciones)
    {
        if (string.IsNullOrWhiteSpace(observaciones))
        {
            return null;
        }

        string valor = observaciones.Trim();
        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Las observaciones no pueden superar 500 caracteres.");
        }

        return valor;
    }

    private static List<LineaSolicitudRequestDto> ValidarLineas(List<LineaSolicitudRequestDto>? lineas)
    {
        if (lineas is null || lineas.Count == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La solicitud debe tener al menos una línea.");
        }

        HashSet<int> productosUnicos = new HashSet<int>();
        foreach (LineaSolicitudRequestDto? linea in lineas)
        {
            if (linea is null)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "La solicitud debe tener al menos una línea.");
            }

            if (linea.Cantidad <= 0)
            {
                throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La cantidad debe ser mayor que 0.");
            }

            if (linea.PrecioEstimado < 0)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "El precio estimado no puede ser negativo.");
            }

            if (!productosUnicos.Add(linea.ProductoId))
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    $"El producto {linea.ProductoId} está repetido en la solicitud.");
            }
        }

        return lineas;
    }
}
