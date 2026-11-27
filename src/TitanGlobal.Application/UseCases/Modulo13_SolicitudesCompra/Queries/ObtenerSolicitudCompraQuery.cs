using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra.Queries;

public class ObtenerSolicitudCompraQuery : IRequest<SolicitudDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerSolicitudCompraQueryHandler : IRequestHandler<ObtenerSolicitudCompraQuery, SolicitudDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerSolicitudCompraQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SolicitudDetalleDto> Handle(ObtenerSolicitudCompraQuery request, CancellationToken cancellationToken)
    {
        SolicitudDetalleDto? solicitud = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.Id)
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
                CodigosEstado.Status404NotFound,
                $"La solicitud de compra {request.Id} no existe.");
        }

        return solicitud;
    }
}
