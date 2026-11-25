using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

internal static class ConsultasCuentasPorPagar
{
    internal static IQueryable<CuentaPorPagar> Cuentas(IUnitOfWork unitOfWork, int? proveedorId = null, string? moneda = null)
    {
        if (proveedorId <= 0)
        {
            throw ValidacionCuentasPorPagar.DatosInvalidos();
        }

        IQueryable<CuentaPorPagar> consulta = unitOfWork.Repository<CuentaPorPagar>().AsQueryable().AsNoTracking();
        if (proveedorId.HasValue)
        {
            consulta = consulta.Where(cuenta => cuenta.ProveedorId == proveedorId.Value);
        }
        if (!string.IsNullOrWhiteSpace(moneda))
        {
            string monedaFiltro = ValidacionCuentasPorPagar.Moneda(moneda);
            consulta = consulta.Where(cuenta => cuenta.Moneda == monedaFiltro);
        }

        return consulta;
    }

    internal static IQueryable<CuentaPorPagarConSaldo> ConSaldos(IQueryable<CuentaPorPagar> cuentas)
    {
        return cuentas.Select(cuenta => new
        {
            Cuenta = cuenta,
            Pagado = cuenta.AplicacionesPago.Where(pago => pago.FechaReversion == null).Sum(pago => pago.Importe)
        }).Select(fila => new CuentaPorPagarConSaldo
        {
            Cuenta = fila.Cuenta,
            ImportePagado = fila.Pagado,
            Saldo = fila.Cuenta.FechaAnulacion == null ? fila.Cuenta.Total - fila.Pagado : 0m
        });
    }

    internal static async Task<CuentaPorPagarConSaldo> CuentaAsync(IUnitOfWork unitOfWork, int id)
    {
        return await ConSaldos(Cuentas(unitOfWork).Where(cuenta => cuenta.Id == id)).SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "La cuenta por pagar no existe.");
    }

    internal static async Task<CuentaPorPagarDetalleDto> DetalleAsync(IUnitOfWork unitOfWork, int id, DateOnly corte)
    {
        CuentaPorPagarConSaldo datos = await CuentaAsync(unitOfWork, id);
        CuentaPorPagar cuenta = datos.Cuenta;
        return new CuentaPorPagarDetalleDto
        {
            Id = cuenta.Id,
            ProveedorId = cuenta.ProveedorId,
            OrdenAbastecimientoId = cuenta.OrdenAbastecimientoId,
            Serie = cuenta.Serie,
            NumeroDocumento = cuenta.NumeroDocumento,
            FechaDocumento = cuenta.FechaDocumento,
            FechaVencimiento = cuenta.FechaVencimiento,
            Moneda = cuenta.Moneda,
            BaseImponible = cuenta.BaseImponible,
            ImporteImpuesto = cuenta.ImporteImpuesto,
            Total = cuenta.Total,
            ImportePagado = datos.ImportePagado,
            Saldo = datos.Saldo,
            Anulada = cuenta.FechaAnulacion.HasValue,
            Vencida = datos.Saldo > 0m && cuenta.FechaVencimiento < corte,
            FechaAnulacion = cuenta.FechaAnulacion,
            MotivoAnulacion = cuenta.MotivoAnulacion
        };
    }

    internal static IQueryable<AplicacionPago> Pagos(IUnitOfWork unitOfWork, int cuentaId)
    {
        return unitOfWork.Repository<AplicacionPago>().AsQueryable().AsNoTracking().Where(pago => pago.CuentaPorPagarId == cuentaId);
    }

    internal static IQueryable<AplicacionPagoDto> ProyectarPagos(IQueryable<AplicacionPago> pagos)
    {
        return pagos.Select(pago => new AplicacionPagoDto
        {
            Id = pago.Id,
            CuentaPorPagarId = pago.CuentaPorPagarId,
            Importe = pago.Importe,
            Moneda = pago.CuentaPorPagar.Moneda,
            FechaPago = pago.FechaPago,
            FechaReversion = pago.FechaReversion,
            MotivoReversion = pago.MotivoReversion
        });
    }
}

internal sealed class CuentaPorPagarConSaldo
{
    public CuentaPorPagar Cuenta { get; init; } = null!;
    public decimal ImportePagado { get; init; }
    public decimal Saldo { get; init; }
}
