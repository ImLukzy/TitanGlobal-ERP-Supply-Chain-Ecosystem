using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Commands;

public class DescontarCuotasCommand : IRequest<decimal>
{
    public int EmpleadoId { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public decimal MontoMaximo { get; set; }

    public int PlanillaId { get; set; }
}

public class DescontarCuotasCommandHandler : IRequestHandler<DescontarCuotasCommand, decimal>
{
    private readonly IUnitOfWork _unidad;

    public DescontarCuotasCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // Operación para Planillas (66): descuenta las cuotas pendientes con mes <= (anio, mes), en orden, sin superar
    // montoMaximo ni el saldo de cada préstamo. No guarda: quien la llama invoca CompleteAsync en su transacción.
    // Devuelve el total descontado.
    public async Task<decimal> Handle(DescontarCuotasCommand request, CancellationToken cancellationToken)
    {
        decimal restante = request.MontoMaximo;
        decimal descontado = 0m;
        if (restante <= 0m)
        {
            return descontado;
        }

        int hasta = request.Anio * 12 + request.Mes;
        List<CuotaPrestamo> cuotas = await _unidad.Repository<CuotaPrestamo>().AsQueryable()
            .Include(cuota => cuota.Prestamo)
            .Where(cuota => cuota.Estado == ValidacionPrestamo.CuotaPendiente
                && cuota.Prestamo.EmpleadoId == request.EmpleadoId
                && cuota.Prestamo.Estado == ValidacionPrestamo.EstadoActivo
                && cuota.Anio * 12 + cuota.Mes <= hasta)
            .OrderBy(cuota => cuota.Anio)
            .ThenBy(cuota => cuota.Mes)
            .ThenBy(cuota => cuota.PrestamoId)
            .ThenBy(cuota => cuota.NumeroCuota)
            .ToListAsync();
        foreach (CuotaPrestamo cuota in cuotas)
        {
            if (restante <= 0m)
            {
                break;
            }

            Prestamo prestamo = cuota.Prestamo;
            decimal pendiente = cuota.Monto - cuota.MontoDescontado;
            decimal importe = Math.Min(Math.Min(pendiente, restante), prestamo.Saldo);
            if (importe <= 0m)
            {
                continue;
            }

            cuota.MontoDescontado += importe;
            await _unidad.Repository<DescuentoCuotaPrestamo>().AddAsync(new DescuentoCuotaPrestamo { CuotaPrestamo = cuota, PlanillaId = request.PlanillaId, Monto = importe });
            if (cuota.MontoDescontado >= cuota.Monto)
            {
                cuota.Estado = ValidacionPrestamo.CuotaDescontada;
            }

            prestamo.Saldo -= importe;
            if (prestamo.Saldo <= 0m)
            {
                prestamo.Estado = ValidacionPrestamo.EstadoCancelado;
            }

            restante -= importe;
            descontado += importe;
        }

        return descontado;
    }
}
