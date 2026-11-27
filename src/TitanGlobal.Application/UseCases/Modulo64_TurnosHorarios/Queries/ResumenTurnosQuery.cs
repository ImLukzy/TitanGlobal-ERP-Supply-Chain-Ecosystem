using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Queries;

public class ResumenTurnosQuery : IRequest<ResumenTurnosDto>
{
}

public class ResumenTurnosQueryHandler : IRequestHandler<ResumenTurnosQuery, ResumenTurnosDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenTurnosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenTurnosDto> Handle(ResumenTurnosQuery request, CancellationToken cancellationToken)
    {
        DateOnly hoy = ValidacionTurno.Hoy();
        IQueryable<Turno> turnos = _unidad.Repository<Turno>().AsQueryable();
        int totalTurnos = await turnos.CountAsync();
        int activos = await turnos.CountAsync(turno => turno.Activo);
        int fijos = await turnos.CountAsync(turno => turno.Tipo == CalculadoraTurnos.TipoFijo);
        int rotativos = await turnos.CountAsync(turno => turno.Tipo == CalculadoraTurnos.TipoRotativo);
        int conTurnoHoy = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .Where(asignacion => asignacion.FechaInicio <= hoy
                && (asignacion.FechaFin == null || asignacion.FechaFin >= hoy)
                && asignacion.Empleado.Estado != ValidacionTurno.EstadoEmpleadoCesado)
            .Select(asignacion => asignacion.EmpleadoId)
            .Distinct()
            .CountAsync();
        int empleadosActivos = await _unidad.Repository<Empleado>().AsQueryable()
            .CountAsync(empleado => empleado.Estado != ValidacionTurno.EstadoEmpleadoCesado);
        return new ResumenTurnosDto
        {
            TotalTurnos = totalTurnos,
            Activos = activos,
            Fijos = fijos,
            Rotativos = rotativos,
            EmpleadosConTurnoHoy = conTurnoHoy,
            EmpleadosActivosSinTurnoHoy = empleadosActivos - conTurnoHoy
        };
    }
}
