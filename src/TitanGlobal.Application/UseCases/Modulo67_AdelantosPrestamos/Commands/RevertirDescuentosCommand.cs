using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Commands;

public class RevertirDescuentosCommand : IRequest<decimal>
{
    public int PlanillaId { get; set; }
}

public class RevertirDescuentosCommandHandler : IRequestHandler<RevertirDescuentosCommand, decimal>
{
    private readonly IUnitOfWork _unidad;

    public RevertirDescuentosCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // Deshace lo descontado por una planilla (el 66 la llama al recalcular una planilla en Borrador o Calculada):
    // borra solo los descuentos de esa planilla y resta exactamente esos importes de la cuota y del saldo.
    // No guarda. Devuelve el total revertido.
    public async Task<decimal> Handle(RevertirDescuentosCommand request, CancellationToken cancellationToken)
    {
        List<DescuentoCuotaPrestamo> descuentos = await _unidad.Repository<DescuentoCuotaPrestamo>().AsQueryable()
            .Include(descuento => descuento.CuotaPrestamo)
            .ThenInclude(cuota => cuota.Prestamo)
            .Where(descuento => descuento.PlanillaId == request.PlanillaId)
            .ToListAsync();
        decimal revertido = 0m;
        foreach (DescuentoCuotaPrestamo descuento in descuentos)
        {
            CuotaPrestamo cuota = descuento.CuotaPrestamo;
            Prestamo prestamo = cuota.Prestamo;
            cuota.MontoDescontado -= descuento.Monto;
            if (cuota.MontoDescontado < cuota.Monto)
            {
                cuota.Estado = ValidacionPrestamo.CuotaPendiente;
            }

            prestamo.Saldo += descuento.Monto;
            if (prestamo.Estado == ValidacionPrestamo.EstadoCancelado)
            {
                prestamo.Estado = ValidacionPrestamo.EstadoActivo;
            }

            revertido += descuento.Monto;
            _unidad.Repository<DescuentoCuotaPrestamo>().Remove(descuento);
        }

        return revertido;
    }
}
