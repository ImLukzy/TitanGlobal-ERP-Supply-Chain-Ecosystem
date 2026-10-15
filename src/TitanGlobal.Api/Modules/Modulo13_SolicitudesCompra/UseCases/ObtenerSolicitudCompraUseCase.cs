using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.UseCases;

public class ObtenerSolicitudCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerSolicitudCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SolicitudDetalleDto> ExecuteAsync(int id)
    {
        SolicitudDetalleDto? solicitud = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new SolicitudDetalleDto
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                Fecha = entidad.Fecha,
                Solicitante = entidad.Solicitante,
                ProveedorId = entidad.ProveedorId,
                ProveedorNombre = entidad.Proveedor == null ? null : entidad.Proveedor.RazonSocial,
                Estado = entidad.Estado,
                Observaciones = entidad.Observaciones,
                MotivoRechazo = entidad.MotivoRechazo,
                FechaDecision = entidad.FechaDecision,
                MontoEstimado = entidad.Detalles.Sum(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioEstimado)) ?? 0m,
                Lineas = entidad.Detalles
                    .OrderBy(detalle => detalle.Id)
                    .Select(detalle => new LineaSolicitudDto
                    {
                        ProductoId = detalle.ProductoId,
                        ProductoNombre = detalle.Producto.Nombre,
                        Cantidad = detalle.Cantidad,
                        PrecioEstimado = detalle.PrecioEstimado,
                        Subtotal = detalle.Cantidad * detalle.PrecioEstimado
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();

        if (solicitud is null)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                $"La solicitud de compra {id} no existe.");
        }

        return solicitud;
    }
}
