using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa;

public static class ValidacionReclamo
{
    private static readonly string[] TiposValidos = new[]
    {
        "Defecto",
        "Retraso",
        "Faltante",
        "Facturacion",
        "Otro"
    };

    private static readonly string[] PrioridadesValidas = new[]
    {
        "Alta",
        "Media",
        "Baja"
    };

    private static readonly string[] EstadosValidos = new[]
    {
        "Abierto",
        "EnProceso",
        "Resuelto",
        "Cerrado",
        "Rechazado"
    };

    public static async Task<GuardarReclamoRequestDto> ValidarAsync(
        GuardarReclamoRequestDto solicitud,
        IUnitOfWork unitOfWork)
    {
        bool clienteExiste = await unitOfWork.Repository<Cliente>().AsQueryable()
            .AnyAsync(cliente => cliente.Id == solicitud.ClienteId);
        if (!clienteExiste)
        {
            throw Error(CodigosEstado.Status400BadRequest, $"El cliente {solicitud.ClienteId} no existe.");
        }

        OrdenCompra? orden = await unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(entidad => entidad.Id == solicitud.OrdenCompraId)
            .Select(entidad => new OrdenCompra
            {
                Id = entidad.Id,
                NumeroOrden = entidad.NumeroOrden,
                ClienteId = entidad.ClienteId
            })
            .SingleOrDefaultAsync();
        if (orden is null)
        {
            throw Error(CodigosEstado.Status400BadRequest, $"La orden de compra {solicitud.OrdenCompraId} no existe.");
        }

        if (orden.ClienteId != solicitud.ClienteId)
        {
            throw Error(CodigosEstado.Status400BadRequest, $"La orden {orden.NumeroOrden} no pertenece al cliente.");
        }

        if (solicitud.ProductoId.HasValue)
        {
            bool productoExiste = await unitOfWork.Repository<Producto>().AsQueryable()
                .AnyAsync(producto => producto.Id == solicitud.ProductoId.Value);
            if (!productoExiste)
            {
                throw Error(CodigosEstado.Status400BadRequest, $"El producto {solicitud.ProductoId.Value} no existe.");
            }

            bool productoEstaEnOrden = await unitOfWork.Repository<DetalleOrdenCompra>().AsQueryable()
                .AnyAsync(detalle => detalle.OrdenCompraId == solicitud.OrdenCompraId
                    && detalle.ProductoId == solicitud.ProductoId.Value);
            if (!productoEstaEnOrden)
            {
                throw Error(CodigosEstado.Status400BadRequest,
                    $"El producto {solicitud.ProductoId.Value} no está en la orden {orden.NumeroOrden}.");
            }
        }

        solicitud.Tipo = NormalizarTipo(solicitud.Tipo);
        solicitud.Prioridad = NormalizarPrioridad(solicitud.Prioridad);
        solicitud.Descripcion = NormalizarDescripcion(solicitud.Descripcion);
        return solicitud;
    }

    public static string? NormalizarEstadoFiltro(string? estado)
    {
        return NormalizarFiltro(estado, EstadosValidos, "El estado no es válido.");
    }

    public static string? NormalizarTipoFiltro(string? tipo)
    {
        return NormalizarFiltro(tipo, TiposValidos, "El tipo de reclamo no es válido.");
    }

    public static string? NormalizarPrioridadFiltro(string? prioridad)
    {
        return NormalizarFiltro(prioridad, PrioridadesValidas, "La prioridad no es válida.");
    }

    public static string NormalizarResolucion(string? resolucion)
    {
        string valor = resolucion?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw Error(CodigosEstado.Status400BadRequest, "La resolución es obligatoria.");
        }

        if (valor.Length > 1000)
        {
            throw Error(CodigosEstado.Status400BadRequest, "La resolución no puede superar 1000 caracteres.");
        }

        return valor;
    }

    public static string NormalizarMotivo(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw Error(CodigosEstado.Status400BadRequest, "El motivo de rechazo es obligatorio.");
        }

        if (valor.Length > 500)
        {
            throw Error(CodigosEstado.Status400BadRequest, "El motivo de rechazo no puede superar 500 caracteres.");
        }

        return valor;
    }

    public static string EstadoLegible(string estado)
    {
        if (estado == "EnProceso")
        {
            return "en proceso";
        }

        return estado.ToLowerInvariant();
    }

    private static string NormalizarTipo(string? tipo)
    {
        string? tipoNormalizado = NormalizarFiltro(tipo, TiposValidos, "El tipo de reclamo no es válido.");
        if (tipoNormalizado is null)
        {
            throw Error(CodigosEstado.Status400BadRequest, "El tipo de reclamo no es válido.");
        }

        return tipoNormalizado!;
    }

    private static string NormalizarPrioridad(string? prioridad)
    {
        string? prioridadNormalizada = NormalizarFiltro(prioridad, PrioridadesValidas, "La prioridad no es válida.");
        if (prioridadNormalizada is null)
        {
            throw Error(CodigosEstado.Status400BadRequest, "La prioridad no es válida.");
        }

        return prioridadNormalizada!;
    }

    private static string NormalizarDescripcion(string? descripcion)
    {
        string valor = descripcion?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw Error(CodigosEstado.Status400BadRequest, "La descripción es obligatoria.");
        }

        if (valor.Length > 1000)
        {
            throw Error(CodigosEstado.Status400BadRequest, "La descripción no puede superar 1000 caracteres.");
        }

        return valor;
    }

    private static string? NormalizarFiltro(string? valor, string[] valoresValidos, string mensajeError)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        string normalizado = valor.Trim();
        foreach (string valorValido in valoresValidos)
        {
            if (normalizado.Equals(valorValido, StringComparison.OrdinalIgnoreCase))
            {
                return valorValido;
            }
        }

        throw Error(CodigosEstado.Status400BadRequest, mensajeError);
    }

    private static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }
}
