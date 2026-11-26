using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

internal static class ConsultasTesoreriaBancos
{
    internal static IQueryable<CuentaBancaria> Cuentas(IUnitOfWork unitOfWork)
    {
        return unitOfWork.Repository<CuentaBancaria>().AsQueryable().AsNoTracking();
    }

    internal static IQueryable<MovimientoBancario> Movimientos(IUnitOfWork unitOfWork, int cuentaId)
    {
        return unitOfWork.Repository<MovimientoBancario>().AsQueryable().AsNoTracking()
            .Where(movimiento => movimiento.CuentaBancariaId == cuentaId);
    }

    internal static async Task<CuentaBancaria> CuentaAsync(IUnitOfWork unitOfWork, int id)
    {
        return await Cuentas(unitOfWork).SingleOrDefaultAsync(cuenta => cuenta.Id == id)
            ?? throw CuentaInexistente();
    }

    internal static IQueryable<CuentaConSaldo> CuentasConSaldo(IQueryable<CuentaBancaria> cuentas)
    {
        return cuentas.Select(cuenta => new CuentaConSaldo
        {
            Cuenta = cuenta,
            Saldo = cuenta.MovimientosBancarios.Where(movimiento => movimiento.FechaReversion == null)
                .Sum(movimiento => movimiento.Tipo == ValidacionTesoreriaBancos.Ingreso ? movimiento.Importe : -movimiento.Importe)
        });
    }

    internal static Task<decimal> SaldoAsync(IUnitOfWork unitOfWork, int id)
    {
        return CuentasConSaldo(Cuentas(unitOfWork).Where(cuenta => cuenta.Id == id))
            .Select(datos => datos.Saldo).SingleAsync();
    }

    internal static async Task<CuentaBancariaDetalleDto> DetalleCuentaAsync(IUnitOfWork unitOfWork, int id)
    {
        CuentaConSaldo datos = await CuentasConSaldo(Cuentas(unitOfWork).Where(cuenta => cuenta.Id == id))
            .SingleOrDefaultAsync() ?? throw CuentaInexistente();
        return new CuentaBancariaDetalleDto
        {
            Id = datos.Cuenta.Id,
            Banco = datos.Cuenta.Banco,
            Numero = datos.Cuenta.Numero,
            Nombre = datos.Cuenta.Nombre,
            Tipo = datos.Cuenta.Tipo,
            Moneda = datos.Cuenta.Moneda,
            Saldo = datos.Saldo,
            FechaRegistro = datos.Cuenta.FechaRegistro
        };
    }

    internal static IQueryable<MovimientoBancarioDto> ProyectarMovimientos(IQueryable<MovimientoBancario> consulta)
    {
        return consulta.Select(movimiento => new MovimientoBancarioDto
        {
            Id = movimiento.Id,
            CuentaBancariaId = movimiento.CuentaBancariaId,
            Moneda = movimiento.CuentaBancaria.Moneda,
            Tipo = movimiento.Tipo,
            Importe = movimiento.Importe,
            FechaOperacion = movimiento.FechaOperacion,
            Referencia = movimiento.Referencia,
            TransferenciaBancariaId = movimiento.TransferenciaBancariaId,
            FechaReversion = movimiento.FechaReversion,
            MotivoReversion = movimiento.MotivoReversion
        });
    }

    internal static async Task<TransferenciaBancariaDto> DetalleTransferenciaAsync(IUnitOfWork unitOfWork, int id)
    {
        return await unitOfWork.Repository<TransferenciaBancaria>().AsQueryable().AsNoTracking()
            .Where(transferencia => transferencia.Id == id)
            .Select(transferencia => new TransferenciaBancariaDto
            {
                Id = transferencia.Id,
                CuentaOrigenId = transferencia.CuentaOrigenId,
                CuentaDestinoId = transferencia.CuentaDestinoId,
                Moneda = transferencia.CuentaOrigen.Moneda,
                Importe = transferencia.Importe,
                FechaOperacion = transferencia.FechaOperacion,
                MovimientoEgresoId = transferencia.MovimientosBancarios
                    .Where(movimiento => movimiento.Tipo == ValidacionTesoreriaBancos.Egreso)
                    .Select(movimiento => movimiento.Id).FirstOrDefault(),
                MovimientoIngresoId = transferencia.MovimientosBancarios
                    .Where(movimiento => movimiento.Tipo == ValidacionTesoreriaBancos.Ingreso)
                    .Select(movimiento => movimiento.Id).FirstOrDefault()
            }).SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "La transferencia bancaria no existe.");
    }

    private static ReglaNegocioException CuentaInexistente()
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, "La cuenta bancaria no existe.");
    }
}

internal sealed class CuentaConSaldo
{
    public CuentaBancaria Cuenta { get; init; } = null!;
    public decimal Saldo { get; init; }
}
