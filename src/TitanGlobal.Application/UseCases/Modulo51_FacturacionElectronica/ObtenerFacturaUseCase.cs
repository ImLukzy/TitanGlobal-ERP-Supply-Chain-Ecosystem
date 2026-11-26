using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class ObtenerFacturaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerFacturaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FacturaDetalleDto> ExecuteAsync(int id)
    {
        IGenericRepository<Factura> repositorio =
            _unitOfWork.Repository<Factura>();

        FacturaDetalleDto? factura = await repositorio
            .AsQueryable()
            .AsNoTracking()
            .Where(factura => factura.Id == id)
            .Select(factura => new FacturaDetalleDto
            {
                Id = factura.Id,
                ClienteId = factura.ClienteId,
                ClienteNombre = factura.Cliente.Nombre,
                ClienteRuc = factura.Cliente.Ruc,
                OrdenCompraId = factura.OrdenCompraId,

                NumeroOrdenCompra = factura.OrdenCompra != null
                    ? factura.OrdenCompra.NumeroOrden
                    : null,

                TipoComprobante = factura.TipoComprobante,
                Serie = factura.Serie,
                Numero = factura.Numero,

                NumeroComprobante =
                    factura.Serie + "-" + factura.Numero,

                FechaEmision = factura.FechaEmision,
                FechaVencimiento = factura.FechaVencimiento,
                Moneda = factura.Moneda,
                Estado = factura.Estado,
                Subtotal = factura.Subtotal,
                TotalDescuento = factura.TotalDescuento,
                TotalImpuesto = factura.TotalImpuesto,
                Total = factura.Total,
                Observaciones = factura.Observaciones,
                FechaCreacion = factura.FechaCreacion,
                FechaAnulacion = factura.FechaAnulacion,
                MotivoAnulacion = factura.MotivoAnulacion,

                Detalles = factura.DetallesFactura
                    .OrderBy(detalle => detalle.Id)
                    .Select(detalle => new DetalleFacturaDto
                    {
                        Id = detalle.Id,
                        ProductoId = detalle.ProductoId,
                        ProductoNombre = detalle.Producto.Nombre,
                        ImpuestoId = detalle.ImpuestoId,

                        ImpuestoNombre = detalle.Impuesto != null
                            ? detalle.Impuesto.Nombre
                            : null,

                        Descripcion = detalle.Descripcion,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario,
                        DescuentoPorcentaje = detalle.DescuentoPorcentaje,
                        PorcentajeImpuesto = detalle.PorcentajeImpuesto,
                        Subtotal = detalle.Subtotal,
                        ImporteDescuento = detalle.ImporteDescuento,
                        BaseImponible = detalle.BaseImponible,
                        ImporteImpuesto = detalle.ImporteImpuesto,
                        Total = detalle.Total
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (factura == null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "La factura solicitada no existe.");
        }

        factura.NumeroComprobante =
            $"{factura.Serie}-{factura.Numero:D8}";

        return factura;
    }
}
