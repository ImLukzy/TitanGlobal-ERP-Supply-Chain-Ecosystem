using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Queries;

public class ResumenDevolucionesComercialesQuery : IRequest<ResumenDevolucionesDto>
{
}

public class ResumenDevolucionesComercialesQueryHandler : IRequestHandler<ResumenDevolucionesComercialesQuery, ResumenDevolucionesDto>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public ResumenDevolucionesComercialesQueryHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<ResumenDevolucionesDto> Handle(ResumenDevolucionesComercialesQuery request, CancellationToken cancellationToken)
    {
        List<DevolucionComercial> devoluciones = await _unidadTrabajo.Repository<DevolucionComercial>()
            .AsQueryable()
            .ToListAsync();
        List<int> idsRecibidas = devoluciones
            .Where(devolucion => devolucion.Estado == "Recibida")
            .Select(devolucion => devolucion.Id)
            .ToList();
        List<DetalleDevolucion> detalles = await _unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .ToListAsync();
        List<DetalleOrdenCompra> detallesOrdenes = await _unidadTrabajo.Repository<DetalleOrdenCompra>()
            .AsQueryable()
            .Select(detalle => new DetalleOrdenCompra
            {
                OrdenCompraId = detalle.OrdenCompraId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            })
            .ToListAsync();
        Dictionary<(int OrdenCompraId, int ProductoId), decimal> precios = OperacionesDevolucionesComerciales.CalcularPreciosUnitarios(detallesOrdenes);
        Dictionary<int, int> ordenesDevoluciones = devoluciones.ToDictionary(devolucion => devolucion.Id, devolucion => devolucion.OrdenCompraId);
        decimal montoReembolsado = detalles
            .Where(detalle => idsRecibidas.Contains(detalle.DevolucionComercialId))
            .Sum(detalle => detalle.Cantidad
                * precios.GetValueOrDefault((ordenesDevoluciones[detalle.DevolucionComercialId], detalle.ProductoId)));
        int unidadesRecibidas = detalles
            .Where(detalle => idsRecibidas.Contains(detalle.DevolucionComercialId))
            .Sum(detalle => detalle.Cantidad);
        int unidadesVendidas = detallesOrdenes.Sum(detalle => detalle.Cantidad);
        decimal tasa = unidadesVendidas == 0
            ? 0m
            : decimal.Round(unidadesRecibidas * 100m / unidadesVendidas, 2, MidpointRounding.AwayFromZero);
        List<MotivoDevolucionesDto> porMotivo = new List<MotivoDevolucionesDto>();
        foreach (string motivo in OrdenMotivos)
        {
            List<DevolucionComercial> porMotivoActual = devoluciones
                .Where(devolucion => devolucion.Motivo == motivo)
                .ToList();
            if (porMotivoActual.Count == 0)
            {
                continue;
            }

            List<int> idsMotivo = porMotivoActual.Select(devolucion => devolucion.Id).ToList();
            porMotivo.Add(new MotivoDevolucionesDto
            {
                Motivo = motivo,
                Devoluciones = porMotivoActual.Count,
                Unidades = detalles.Where(detalle => idsMotivo.Contains(detalle.DevolucionComercialId))
                    .Sum(detalle => detalle.Cantidad)
            });
        }

        return new ResumenDevolucionesDto
        {
            Total = devoluciones.Count,
            Solicitadas = devoluciones.Count(devolucion => devolucion.Estado == "Solicitada"),
            Aprobadas = devoluciones.Count(devolucion => devolucion.Estado == "Aprobada"),
            Recibidas = devoluciones.Count(devolucion => devolucion.Estado == "Recibida"),
            Rechazadas = devoluciones.Count(devolucion => devolucion.Estado == "Rechazada"),
            MontoReembolsado = montoReembolsado,
            PorMotivo = porMotivo,
            TasaDevolucion = tasa
        };
    }

    private static readonly string[] OrdenMotivos = new[]
    {
        "Defecto",
        "ErrorPedido",
        "Arrepentimiento",
        "Otro"
    };
}
