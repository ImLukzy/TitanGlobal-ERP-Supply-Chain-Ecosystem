using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class ResumirCuentasPorPagarUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    public ResumirCuentasPorPagarUseCase(IUnitOfWork unitOfWork, TimeProvider reloj)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public async Task<ResumenCuentasPorPagarDto> ExecuteAsync(int? proveedorId, string? moneda)
    {
        DateOnly corte = ValidacionCuentasPorPagar.CorteLima(_reloj);
        DateOnly limite30 = corte.AddDays(-30);
        DateOnly limite60 = corte.AddDays(-60);
        DateOnly limite90 = corte.AddDays(-90);
        List<ResumenProveedorMonedaDto> grupos = await ConsultasCuentasPorPagar.ConSaldos(
            ConsultasCuentasPorPagar.Cuentas(_unitOfWork, proveedorId, moneda))
            .Where(fila => fila.Saldo > 0m)
            .GroupBy(fila => new { fila.Cuenta.ProveedorId, fila.Cuenta.Moneda })
            .Select(grupo => new ResumenProveedorMonedaDto
            {
                ProveedorId = grupo.Key.ProveedorId,
                Moneda = grupo.Key.Moneda,
                SaldoAlDia = grupo.Sum(fila => fila.Cuenta.FechaVencimiento >= corte ? fila.Saldo : 0m),
                SaldoVencido = grupo.Sum(fila => fila.Cuenta.FechaVencimiento < corte ? fila.Saldo : 0m),
                Vencido0A30 = grupo.Sum(fila => fila.Cuenta.FechaVencimiento >= limite30 && fila.Cuenta.FechaVencimiento < corte ? fila.Saldo : 0m),
                Vencido31A60 = grupo.Sum(fila => fila.Cuenta.FechaVencimiento >= limite60 && fila.Cuenta.FechaVencimiento < limite30 ? fila.Saldo : 0m),
                Vencido61A90 = grupo.Sum(fila => fila.Cuenta.FechaVencimiento >= limite90 && fila.Cuenta.FechaVencimiento < limite60 ? fila.Saldo : 0m),
                VencidoMasDe90 = grupo.Sum(fila => fila.Cuenta.FechaVencimiento < limite90 ? fila.Saldo : 0m)
            }).OrderBy(grupo => grupo.ProveedorId).ThenBy(grupo => grupo.Moneda).ToListAsync();

        return new ResumenCuentasPorPagarDto { FechaCorte = corte, Grupos = grupos };
    }
}
