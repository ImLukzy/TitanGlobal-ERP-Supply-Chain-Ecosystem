using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Queries;

public class EvaluarPromocionesQuery : IRequest<EvaluacionCompraDto>
{
    public required EvaluarCompraRequestDto Solicitud { get; set; }
}

public class EvaluarPromocionesQueryHandler : IRequestHandler<EvaluarPromocionesQuery, EvaluacionCompraDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public EvaluarPromocionesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EvaluacionCompraDto> Handle(EvaluarPromocionesQuery request, CancellationToken cancellationToken)
    {
        ClienteClasificadoDto? cliente = await ConsultaSegmentacionClientes.ObtenerClienteClasificadoAsync(
            request.Solicitud.ClienteId, _unitOfWork);
        if (cliente is null)
        {
            throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, $"El cliente {request.Solicitud.ClienteId} no existe.");
        }

        if (request.Solicitud.Lineas is null || request.Solicitud.Lineas.Count == 0)
        {
            throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, "La compra debe tener al menos una línea.");
        }

        foreach (LineaCompraRequestDto linea in request.Solicitud.Lineas)
        {
            if (linea.Cantidad <= 0)
            {
                throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, "La cantidad debe ser mayor que 0.");
            }

            if (linea.PrecioUnitario <= 0m)
            {
                throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, "El precio unitario debe ser mayor que 0.");
            }
        }

        List<int> idsProductos = request.Solicitud.Lineas.Select(linea => linea.ProductoId).Distinct().ToList();
        List<Producto> productos = await _unitOfWork.Repository<Producto>().AsQueryable()
            .Where(producto => idsProductos.Contains(producto.Id)).ToListAsync();
        Dictionary<int, Producto> productosPorId = productos.ToDictionary(producto => producto.Id);
        foreach (LineaCompraRequestDto linea in request.Solicitud.Lineas)
        {
            if (!productosPorId.ContainsKey(linea.ProductoId))
            {
                throw OperacionesPromociones.Error(CodigosEstado.Status400BadRequest, $"El producto {linea.ProductoId} no existe.");
            }
        }

        DateTime fecha = FechasUtc.Normalizar(request.Solicitud.Fecha ?? DateTime.UtcNow).Date;
        List<Promocion> promociones = await _unitOfWork.Repository<Promocion>().AsQueryable()
            .Where(promocion => promocion.Activa
                && promocion.FechaInicio <= fecha
                && promocion.FechaFin >= fecha
                && (!promocion.SegmentoClienteId.HasValue || promocion.SegmentoClienteId == cliente.SegmentoId)
                && (promocion.Tipo == "MontoMinimo"
                    || (promocion.Tipo == "Producto" && promocion.ProductoId.HasValue
                        && idsProductos.Contains(promocion.ProductoId.Value))))
            .ToListAsync();

        List<LineaEvaluadaDto> lineasEvaluadas = new List<LineaEvaluadaDto>();
        foreach (LineaCompraRequestDto linea in request.Solicitud.Lineas)
        {
            Producto producto = productosPorId[linea.ProductoId];
            Promocion? promocionLinea = promociones
                .Where(promocion => promocion.Tipo == "Producto" && promocion.ProductoId == linea.ProductoId)
                .OrderByDescending(promocion => promocion.PorcentajeDescuento)
                .ThenBy(promocion => promocion.Id)
                .FirstOrDefault();
            decimal subtotal = linea.Cantidad * linea.PrecioUnitario;
            decimal descuento = promocionLinea is null
                ? 0m
                : decimal.Round(subtotal * promocionLinea.PorcentajeDescuento / 100m, 2, MidpointRounding.AwayFromZero);
            lineasEvaluadas.Add(new LineaEvaluadaDto
            {
                ProductoId = linea.ProductoId,
                ProductoNombre = producto.Nombre,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                Subtotal = subtotal,
                Promocion = promocionLinea is null ? null : CrearPromocionAplicada(promocionLinea),
                Descuento = descuento,
                Importe = subtotal - descuento
            });
        }

        decimal totalTrasLineas = lineasEvaluadas.Sum(linea => linea.Importe);
        Promocion? promocionCompra = promociones
            .Where(promocion => promocion.Tipo == "MontoMinimo" && promocion.MontoMinimo <= totalTrasLineas)
            .OrderByDescending(promocion => promocion.PorcentajeDescuento)
            .ThenBy(promocion => promocion.Id)
            .FirstOrDefault();
        decimal descuentoCompra = promocionCompra is null
            ? 0m
            : decimal.Round(totalTrasLineas * promocionCompra.PorcentajeDescuento / 100m, 2, MidpointRounding.AwayFromZero);
        decimal subtotalCompra = lineasEvaluadas.Sum(linea => linea.Subtotal);
        decimal descuentoLineas = lineasEvaluadas.Sum(linea => linea.Descuento);
        return new EvaluacionCompraDto
        {
            ClienteId = cliente.ClienteId,
            ClienteNombre = cliente.Nombre,
            SegmentoCodigo = cliente.SegmentoCodigo,
            Fecha = fecha,
            Lineas = lineasEvaluadas,
            TotalTrasLineas = totalTrasLineas,
            PromocionCompra = promocionCompra is null ? null : CrearPromocionAplicada(promocionCompra),
            DescuentoCompra = descuentoCompra,
            Subtotal = subtotalCompra,
            DescuentoTotal = descuentoLineas + descuentoCompra,
            Total = totalTrasLineas - descuentoCompra
        };
    }

    private static PromocionAplicadaDto CrearPromocionAplicada(Promocion promocion)
    {
        return new PromocionAplicadaDto
        {
            Id = promocion.Id,
            Codigo = promocion.Codigo,
            Nombre = promocion.Nombre,
            PorcentajeDescuento = promocion.PorcentajeDescuento
        };
    }
}
