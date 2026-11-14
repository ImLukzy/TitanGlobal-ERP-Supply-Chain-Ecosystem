using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class CalculoFactura
{
    private readonly IUnitOfWork _unitOfWork;

    public CalculoFactura(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<DetalleFactura>> CalcularDetallesAsync(
        GuardarFacturaRequestDto request)
    {
        List<DetalleFactura> detalles = new();

        IGenericRepository<Producto> repositorioProductos =
            _unitOfWork.Repository<Producto>();

        IGenericRepository<Impuesto> repositorioImpuestos =
            _unitOfWork.Repository<Impuesto>();

        IGenericRepository<TasaImpuesto> repositorioTasas =
            _unitOfWork.Repository<TasaImpuesto>();

        foreach (GuardarDetalleFacturaRequestDto linea in request.Detalles)
        {
            Producto? producto = await repositorioProductos
                .AsQueryable()
                .FirstOrDefaultAsync(
                    producto => producto.Id == linea.ProductoId);

            if (producto == null)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status404NotFound,
                    $"No se encontró el producto con ID {linea.ProductoId}.");
            }

            decimal subtotal = ValidacionFactura.RedondearImporte(
                linea.Cantidad * linea.PrecioUnitario);

            decimal importeDescuento =
                ValidacionFactura.RedondearImporte(
                    subtotal * linea.DescuentoPorcentaje / 100m);

            decimal baseImponible =
                subtotal - importeDescuento;

            decimal porcentajeImpuesto = 0m;

            decimal importeImpuesto = 0m;

            if (linea.ImpuestoId.HasValue)
            {
                Impuesto? impuesto = await repositorioImpuestos
                    .AsQueryable()
                    .FirstOrDefaultAsync(
                        impuesto => impuesto.Id == linea.ImpuestoId.Value);

                if (impuesto == null)
                {
                    throw new ReglaNegocioException(
                        CodigosEstado.Status404NotFound,
                        $"No se encontró el impuesto con ID {linea.ImpuestoId.Value}.");
                }

                if (!impuesto.Activo)
                {
                    throw new ReglaNegocioException(
                        CodigosEstado.Status400BadRequest,
                        "El impuesto seleccionado se encuentra inactivo.");
                }

                if (!string.Equals(
                    impuesto.Codigo,
                    "IGV",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new ReglaNegocioException(
                        CodigosEstado.Status400BadRequest,
                        "Por el momento solo se admite el IGV como impuesto de facturación.");
                }

                DateTime fechaOperacion = DateTime.SpecifyKind(
                    request.FechaEmision!.Value.Date,
                    DateTimeKind.Utc);

                TasaImpuesto? tasa = await repositorioTasas
                    .AsQueryable()
                    .Where(tasa =>
                        tasa.ImpuestoId == impuesto.Id &&
                        tasa.Activo &&
                        tasa.FechaInicio <= fechaOperacion &&
                        (
                            tasa.FechaFin == null ||
                            tasa.FechaFin >= fechaOperacion
                        ))
                    .OrderByDescending(tasa => tasa.FechaInicio)
                    .FirstOrDefaultAsync();

                if (tasa == null)
                {
                    throw new ReglaNegocioException(
                        CodigosEstado.Status400BadRequest,
                        "El impuesto no tiene una tasa vigente para la fecha de emisión.");
                }

                porcentajeImpuesto = tasa.Porcentaje;

                importeImpuesto =
                    ValidacionFactura.RedondearImporte(
                        baseImponible * porcentajeImpuesto / 100m);
            }

            decimal total = baseImponible + importeImpuesto;

            ValidacionFactura.ValidarImporte(subtotal);

            ValidacionFactura.ValidarImporte(importeDescuento);

            ValidacionFactura.ValidarImporte(baseImponible);

            ValidacionFactura.ValidarImporte(importeImpuesto);

            ValidacionFactura.ValidarImporte(total);

            DetalleFactura detalle = new DetalleFactura
            {
                ProductoId = linea.ProductoId,
                ImpuestoId = linea.ImpuestoId,
                Descripcion = linea.Descripcion,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                DescuentoPorcentaje = linea.DescuentoPorcentaje,
                PorcentajeImpuesto = porcentajeImpuesto,
                Subtotal = subtotal,
                ImporteDescuento = importeDescuento,
                BaseImponible = baseImponible,
                ImporteImpuesto = importeImpuesto,
                Total = total
            };

            detalles.Add(detalle);
        }

        return detalles;
    }

    public static void CalcularTotales(Factura factura)
    {
        factura.Subtotal = factura.DetallesFactura
            .Sum(detalle => detalle.Subtotal);

        factura.TotalDescuento = factura.DetallesFactura
            .Sum(detalle => detalle.ImporteDescuento);

        factura.TotalImpuesto = factura.DetallesFactura
            .Sum(detalle => detalle.ImporteImpuesto);

        factura.Total = factura.Subtotal -
                        factura.TotalDescuento +
                        factura.TotalImpuesto;

        ValidacionFactura.ValidarImporte(factura.Subtotal);

        ValidacionFactura.ValidarImporte(factura.TotalDescuento);

        ValidacionFactura.ValidarImporte(factura.TotalImpuesto);

        ValidacionFactura.ValidarImporte(factura.Total);
    }
}
