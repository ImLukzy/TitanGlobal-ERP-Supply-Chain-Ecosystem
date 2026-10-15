using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

public class GenerarOrdenDesdeSolicitudUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GenerarOrdenDesdeSolicitudUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoCreadaDto> ExecuteAsync(
        int solicitudId,
        GenerarDesdeSolicitudRequestDto solicitud)
    {
        SolicitudCompra? solicitudCompra = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .Where(entidad => entidad.Id == solicitudId)
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
                StatusCodes.Status404NotFound,
                $"La solicitud de compra {solicitudId} no existe.");
        }

        if (solicitudCompra.Estado != "Aprobada")
        {
            throw new ReglaNegocioException(
                StatusCodes.Status409Conflict,
                $"La solicitud {solicitudCompra.Numero} no está aprobada.");
        }

        OrdenAbastecimiento? ordenExistente = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .Where(orden => orden.SolicitudCompraId == solicitudId && orden.Estado != "Anulada")
            .Select(orden => new OrdenAbastecimiento
            {
                Numero = orden.Numero
            })
            .SingleOrDefaultAsync();
        if (ordenExistente is not null)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status409Conflict,
                $"La solicitud {solicitudCompra.Numero} ya tiene la orden {ordenExistente.Numero}.");
        }

        int? proveedorId = solicitud.ProveedorId ?? solicitudCompra.ProveedorId;
        if (!proveedorId.HasValue)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "Indica el proveedor de la orden.");
        }

        GuardarOrdenAbastecimientoRequestDto datosOrden = new GuardarOrdenAbastecimientoRequestDto
        {
            ProveedorId = proveedorId.Value,
            FechaEntregaEstimada = solicitud.FechaEntregaEstimada,
            Observaciones = solicitud.Observaciones,
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
            SolicitudCompraId = solicitudId,
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
