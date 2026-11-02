using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales;

public static class ValidacionDevolucion
{
    private static readonly string[] MotivosValidos = new[]
    {
        "Defecto",
        "ErrorPedido",
        "Arrepentimiento",
        "Otro"
    };

    private static readonly string[] EstadosValidos = new[]
    {
        "Solicitada",
        "Aprobada",
        "Recibida",
        "Rechazada"
    };

    public static async Task<GuardarDevolucionRequestDto> ValidarAsync(
        GuardarDevolucionRequestDto solicitud,
        IUnitOfWork unidadTrabajo)
    {
        bool clienteExiste = await unidadTrabajo.Repository<Cliente>().AsQueryable()
            .AnyAsync(cliente => cliente.Id == solicitud.ClienteId);
        if (!clienteExiste)
        {
            throw Error(CodigosEstado.Status400BadRequest, $"El cliente {solicitud.ClienteId} no existe.");
        }

        OrdenCompra? orden = await unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
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
            throw Error(CodigosEstado.Status400BadRequest,
                $"La orden de compra {solicitud.OrdenCompraId} no existe.");
        }

        if (orden.ClienteId != solicitud.ClienteId)
        {
            throw Error(CodigosEstado.Status400BadRequest, $"La orden {orden.NumeroOrden} no pertenece al cliente.");
        }

        if (solicitud.ReclamoPostventaId.HasValue)
        {
            ReclamoPostventa? reclamo = await unidadTrabajo.Repository<ReclamoPostventa>().AsQueryable()
                .Where(entidad => entidad.Id == solicitud.ReclamoPostventaId.Value)
                .Select(entidad => new ReclamoPostventa
                {
                    Id = entidad.Id,
                    Numero = entidad.Numero,
                    OrdenCompraId = entidad.OrdenCompraId
                })
                .SingleOrDefaultAsync();
            if (reclamo is null)
            {
                throw Error(CodigosEstado.Status400BadRequest,
                    $"El reclamo {solicitud.ReclamoPostventaId.Value} no existe.");
            }

            if (reclamo.OrdenCompraId != solicitud.OrdenCompraId)
            {
                throw Error(CodigosEstado.Status400BadRequest,
                    $"El reclamo {reclamo.Numero} no es de la orden {orden.NumeroOrden}.");
            }
        }

        solicitud.Motivo = NormalizarMotivoDevolucion(solicitud.Motivo);
        solicitud.Observaciones = NormalizarObservaciones(solicitud.Observaciones);
        if (solicitud.Lineas is null || solicitud.Lineas.Count == 0)
        {
            throw Error(CodigosEstado.Status400BadRequest, "La devolución debe tener al menos una línea.");
        }

        if (solicitud.Lineas.Any(linea => linea.Cantidad <= 0))
        {
            throw Error(CodigosEstado.Status400BadRequest, "La cantidad debe ser mayor que 0.");
        }

        IGrouping<int, LineaGuardarDevolucionDto>? repetido = solicitud.Lineas
            .GroupBy(linea => linea.ProductoId)
            .FirstOrDefault(grupo => grupo.Count() > 1);
        if (repetido is not null)
        {
            throw Error(CodigosEstado.Status400BadRequest,
                $"El producto {repetido.Key} está repetido en la devolución.");
        }

        List<int> productosIds = solicitud.Lineas.Select(linea => linea.ProductoId).ToList();
        List<int> productosExistentes = await unidadTrabajo.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id))
            .Select(producto => producto.Id)
            .ToListAsync();
        int? productoInexistente = solicitud.Lineas
            .Where(linea => !productosExistentes.Contains(linea.ProductoId))
            .Select(linea => (int?)linea.ProductoId)
            .FirstOrDefault();
        if (productoInexistente.HasValue)
        {
            throw Error(CodigosEstado.Status400BadRequest, $"El producto {productoInexistente.Value} no existe.");
        }

        List<int> productosEnOrden = await unidadTrabajo.Repository<DetalleOrdenCompra>().AsQueryable()
            .Where(detalle => detalle.OrdenCompraId == solicitud.OrdenCompraId
                && productosIds.Contains(detalle.ProductoId))
            .Select(detalle => detalle.ProductoId)
            .Distinct()
            .ToListAsync();
        int? productoAjeno = solicitud.Lineas
            .Where(linea => !productosEnOrden.Contains(linea.ProductoId))
            .Select(linea => (int?)linea.ProductoId)
            .FirstOrDefault();
        if (productoAjeno.HasValue)
        {
            throw Error(CodigosEstado.Status400BadRequest,
                $"El producto {productoAjeno.Value} no está en la orden {orden.NumeroOrden}.");
        }

        return solicitud;
    }

    public static string? NormalizarEstadoFiltro(string? estado)
    {
        return NormalizarFiltro(estado, EstadosValidos, "El estado no es válido.");
    }

    public static string? NormalizarMotivoFiltro(string? motivo)
    {
        return NormalizarFiltro(motivo, MotivosValidos, "El motivo no es válido.");
    }

    public static string NormalizarMotivoRechazo(string? motivo)
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
        return estado.ToLowerInvariant();
    }

    private static string NormalizarMotivoDevolucion(string? motivo)
    {
        string? normalizado = NormalizarFiltro(motivo, MotivosValidos, "El motivo no es válido.");
        if (normalizado is null)
        {
            throw Error(CodigosEstado.Status400BadRequest, "El motivo no es válido.");
        }

        return normalizado;
    }

    private static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (valor.Length > 500)
        {
            throw Error(CodigosEstado.Status400BadRequest, "Las observaciones no pueden superar 500 caracteres.");
        }

        return valor;
    }

    private static string? NormalizarFiltro(string? valor, string[] valoresValidos, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        string? canonico = valoresValidos.FirstOrDefault(item =>
            string.Equals(item, valor.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonico is null)
        {
            throw Error(CodigosEstado.Status400BadRequest, mensaje);
        }

        return canonico;
    }

    private static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }
}
