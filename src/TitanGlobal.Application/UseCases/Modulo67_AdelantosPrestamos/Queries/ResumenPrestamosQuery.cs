using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Queries;

public class ResumenPrestamosQuery : IRequest<ResumenPrestamosDto>
{
}

public class ResumenPrestamosQueryHandler : IRequestHandler<ResumenPrestamosQuery, ResumenPrestamosDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenPrestamosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenPrestamosDto> Handle(ResumenPrestamosQuery request, CancellationToken cancellationToken)
    {
        DateOnly hoy = ValidacionPrestamo.Hoy();
        DateOnly inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        DateOnly finMes = inicioMes.AddMonths(1).AddDays(-1);
        List<FilaPrestamo> prestamos = await _unidad.Repository<Prestamo>().AsQueryable()
            .AsNoTracking()
            .Where(prestamo => prestamo.Estado != ValidacionPrestamo.EstadoAnulado)
            .Select(prestamo => new FilaPrestamo
            {
                Estado = prestamo.Estado,
                Monto = prestamo.Monto,
                Saldo = prestamo.Saldo,
                FechaOtorgamiento = prestamo.FechaOtorgamiento
            })
            .ToListAsync();
        List<CuotaPrestamo> cuotasDelMes = await _unidad.Repository<CuotaPrestamo>().AsQueryable()
            .AsNoTracking()
            .Where(cuota => cuota.Estado == ValidacionPrestamo.CuotaPendiente
                && cuota.Prestamo.Estado == ValidacionPrestamo.EstadoActivo
                && cuota.Anio == hoy.Year
                && cuota.Mes == hoy.Month)
            .ToListAsync();
        List<FilaPrestamo> activos = prestamos.Where(fila => fila.Estado == ValidacionPrestamo.EstadoActivo).ToList();
        return new ResumenPrestamosDto
        {
            Activos = activos.Count,
            SaldoPendienteTotal = activos.Sum(fila => fila.Saldo),
            OtorgadoMes = prestamos
                .Where(fila => fila.FechaOtorgamiento >= inicioMes && fila.FechaOtorgamiento <= finMes)
                .Sum(fila => fila.Monto),
            CuotasDelMes = cuotasDelMes.Sum(cuota => cuota.Monto - cuota.MontoDescontado)
        };
    }

    private sealed class FilaPrestamo
    {
        public string Estado { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public decimal Saldo { get; set; }

        public DateOnly FechaOtorgamiento { get; set; }
    }
}
