using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Commands;

public class GenerarOrdenDesdeSolicitudCommand : IRequest<OrdenAbastecimientoCreadaDto>
{
    public int SolicitudId { get; set; }

    public required GenerarDesdeSolicitudRequestDto Solicitud { get; set; }
}

public class GenerarOrdenDesdeSolicitudCommandHandler : IRequestHandler<GenerarOrdenDesdeSolicitudCommand, OrdenAbastecimientoCreadaDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GenerarOrdenDesdeSolicitudCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoCreadaDto> Handle(GenerarOrdenDesdeSolicitudCommand request, CancellationToken cancellationToken)
    {
        SolicitudCompra? solicitudCompra = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.SolicitudId)
            .Select(entidad => new SolicitudCompra
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                Estado = entidad.Estado,
                ProveedorId = entidad.ProveedorId,
                Detalles = entidad.Detalles.Select(detalle => new DetalleSolicitudCompra
                {
                    ProductoId = detalle.ProductoId,
                    Cantidad = detalle.Cantidad,
                    PrecioEstimado = detalle.PrecioEstimado
                }).ToList()
            })
            .SingleOrDefaultAsync();
        if (solicitudCompra is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La solicitud de compra {request.SolicitudId} no existe.");
        }

        if (solicitudCompra.Estado != "Aprobada")
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La solicitud {solicitudCompra.Numero} no está aprobada.");
        }

        OrdenAbastecimiento? ordenExistente = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .Where(orden => orden.SolicitudCompraId == request.SolicitudId && orden.Estado != "Anulada")
            .Select(orden => new OrdenAbastecimiento
            {
                Numero = orden.Numero
            })
            .SingleOrDefaultAsync();
        if (ordenExistente is not null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La solicitud {solicitudCompra.Numero} ya tiene la orden {ordenExistente.Numero}.");
        }

        int? proveedorId = request.Solicitud.ProveedorId ?? solicitudCompra.ProveedorId;
        if (!proveedorId.HasValue)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Indica el proveedor de la orden.");
        }

        GuardarOrdenAbastecimientoRequestDto datosOrden = new GuardarOrdenAbastecimientoRequestDto
        {
            ProveedorId = proveedorId.Value,
            FechaEntregaEstimada = request.Solicitud.FechaEntregaEstimada,
            Observaciones = request.Solicitud.Observaciones,
            Lineas = solicitudCompra.Detalles.Select(detalle => new LineaOrdenAbastecimientoRequestDto
            {
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioEstimado
            }).ToList()
        };
        GuardarOrdenAbastecimientoRequestDto datosValidados = await ValidacionOrdenAbastecimiento.ValidarAsync(
            datosOrden,
            _unitOfWork);
        OrdenAbastecimiento orden = new OrdenAbastecimiento
        {
            Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
            ProveedorId = datosValidados.ProveedorId,
            SolicitudCompraId = request.SolicitudId,
            Estado = "Emitida",
            FechaEntregaEstimada = datosValidados.FechaEntregaEstimada,
            Observaciones = datosValidados.Observaciones
        };
        foreach (LineaOrdenAbastecimientoRequestDto linea in datosValidados.Lineas!)
        {
            orden.Detalles.Add(new DetalleOrdenAbastecimiento
            {
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario
            });
        }

        return await PersistenciaOrdenAbastecimiento.CrearAsync(orden, _unitOfWork);
    }
}
