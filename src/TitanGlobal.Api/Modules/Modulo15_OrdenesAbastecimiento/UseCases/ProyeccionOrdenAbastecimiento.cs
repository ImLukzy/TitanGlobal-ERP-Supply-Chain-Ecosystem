using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

public static class ProyeccionOrdenAbastecimiento
{
    public static Expression<Func<OrdenAbastecimiento, OrdenAbastecimientoDetalleDto>> CrearSelector(DateTime hoyUtc)
    {
        Expression<Func<OrdenAbastecimiento, OrdenAbastecimientoDetalleDto>> selector = entidad => new OrdenAbastecimientoDetalleDto
        {
            Id = entidad.Id,
            Numero = entidad.Numero,
            Fecha = entidad.Fecha,
            ProveedorId = entidad.ProveedorId,
            ProveedorNombre = entidad.Proveedor.RazonSocial,
            SolicitudCompraId = entidad.SolicitudCompraId,
            SolicitudNumero = entidad.SolicitudCompra == null ? null : entidad.SolicitudCompra.Numero,
            Estado = entidad.Estado,
            FechaEntregaEstimada = entidad.FechaEntregaEstimada,
            FechaRecepcion = entidad.FechaRecepcion,
            Atrasada = entidad.Estado == "Emitida" && entidad.FechaEntregaEstimada < hoyUtc,
            Observaciones = entidad.Observaciones,
            MotivoAnulacion = entidad.MotivoAnulacion,
            MontoTotal = entidad.Detalles.Sum(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioUnitario)) ?? 0m,
            Lineas = entidad.Detalles
                .OrderBy(detalle => detalle.Id)
                .Select(detalle => new LineaOrdenAbastecimientoDto
                {
                    ProductoId = detalle.ProductoId,
                    ProductoNombre = detalle.Producto.Nombre,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    Subtotal = detalle.Cantidad * detalle.PrecioUnitario
                })
                .ToList()
        };
        return selector;
    }

    public static async Task<OrdenAbastecimientoDetalleDto> ObtenerDetalleAsync(int id, IUnitOfWork unitOfWork)
    {
        DateTime hoyUtc = DateTime.UtcNow.Date;
        OrdenAbastecimientoDetalleDto? orden = await unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            // PATRÓN LINQ 10: proyección jerárquica de la orden y sus líneas.
            .Select(CrearSelector(hoyUtc))
            .SingleOrDefaultAsync();
        if (orden is null)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                $"La orden de abastecimiento {id} no existe.");
        }

        return orden;
    }
}
