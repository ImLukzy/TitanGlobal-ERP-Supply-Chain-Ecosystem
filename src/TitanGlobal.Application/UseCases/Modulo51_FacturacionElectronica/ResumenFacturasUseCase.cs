using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class ResumenFacturasUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenFacturasUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenFacturasDto> ExecuteAsync(
        string moneda = "PEN",
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null)
    {
        string monedaNormalizada =
            (moneda ?? string.Empty).Trim().ToUpperInvariant();

        if (monedaNormalizada.Length != 3 ||
            !monedaNormalizada.All(
                caracter => caracter >= 'A' && caracter <= 'Z'))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La moneda debe tener un código de tres letras.");
        }

        DateTime? desde = fechaDesde.HasValue
            ? FechasUtc.Normalizar(fechaDesde.Value)
            : null;

        DateTime? hasta = fechaHasta.HasValue
            ? FechasUtc.Normalizar(fechaHasta.Value)
            : null;

        if (desde.HasValue &&
            hasta.HasValue &&
            desde.Value > hasta.Value)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha inicial no puede ser posterior a la fecha final.");
        }

        IGenericRepository<Factura> repositorio =
            _unitOfWork.Repository<Factura>();

        IQueryable<Factura> consulta = repositorio
            .AsQueryable()
            .AsNoTracking()
            .Where(factura =>
                factura.Moneda == monedaNormalizada);

        if (desde.HasValue)
        {
            consulta = consulta.Where(
                factura => factura.FechaEmision >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(
                factura => factura.FechaEmision <= hasta.Value);
        }

        ResumenFacturasDto? resumen = await consulta
            .GroupBy(factura => 1)
            .Select(grupo => new ResumenFacturasDto
            {
                Moneda = monedaNormalizada,

                TotalComprobantes = grupo.Count(),

                TotalFacturas = grupo.Count(factura =>
                    factura.TipoComprobante ==
                    ValidacionFactura.TipoFactura),

                TotalBoletas = grupo.Count(factura =>
                    factura.TipoComprobante ==
                    ValidacionFactura.TipoBoleta),

                TotalBorradores = grupo.Count(factura =>
                    factura.Estado ==
                    ValidacionFactura.EstadoBorrador),

                TotalEmitidas = grupo.Count(factura =>
                    factura.Estado ==
                    ValidacionFactura.EstadoEmitida),

                TotalAnuladas = grupo.Count(factura =>
                    factura.Estado ==
                    ValidacionFactura.EstadoAnulada),

                SubtotalFacturado = grupo.Sum(factura =>
                    factura.Estado ==
                    ValidacionFactura.EstadoEmitida
                        ? factura.Subtotal
                        : 0m),

                TotalDescuentos = grupo.Sum(factura =>
                    factura.Estado ==
                    ValidacionFactura.EstadoEmitida
                        ? factura.TotalDescuento
                        : 0m),

                TotalImpuestos = grupo.Sum(factura =>
                    factura.Estado ==
                    ValidacionFactura.EstadoEmitida
                        ? factura.TotalImpuesto
                        : 0m),

                TotalFacturado = grupo.Sum(factura =>
                    factura.Estado ==
                    ValidacionFactura.EstadoEmitida
                        ? factura.Total
                        : 0m)
            })
            .FirstOrDefaultAsync();

        if (resumen == null)
        {
            return new ResumenFacturasDto
            {
                Moneda = monedaNormalizada
            };
        }

        return resumen;
    }
}
