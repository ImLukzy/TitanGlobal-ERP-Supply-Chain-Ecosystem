using MediatR;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Queries;

public class ObtenerSaldoQuery : IRequest<SaldoVacacionalDto>
{
    public int EmpleadoId { get; set; }

    public DateOnly? Fecha { get; set; }
}

public class ObtenerSaldoQueryHandler : IRequestHandler<ObtenerSaldoQuery, SaldoVacacionalDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerSaldoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<SaldoVacacionalDto> Handle(ObtenerSaldoQuery request, CancellationToken cancellationToken)
    {
        Empleado empleado = await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId);
        DateOnly dia = request.Fecha ?? ValidacionAusencia.Hoy();
        List<CalculadoraSaldoVacacional.PeriodoVacacional> periodos =
            await ValidacionAusencia.LeerPeriodosAsync(_unidad, request.EmpleadoId, null);
        decimal devengados = CalculadoraSaldoVacacional.Devengados(empleado.FechaIngreso, dia);
        int gozados = CalculadoraSaldoVacacional.Gozados(periodos, dia);
        int programados = CalculadoraSaldoVacacional.Programados(periodos, dia);
        return new SaldoVacacionalDto
        {
            EmpleadoId = empleado.Id,
            FechaIngreso = empleado.FechaIngreso,
            Fecha = dia,
            MesesServicio = CalculadoraSaldoVacacional.MesesCompletos(empleado.FechaIngreso, dia),
            DiasDevengados = decimal.Round(devengados, 1),
            DiasGozados = gozados,
            DiasProgramados = programados,
            Saldo = decimal.Round(CalculadoraSaldoVacacional.Saldo(devengados, gozados, programados), 1)
        };
    }
}
