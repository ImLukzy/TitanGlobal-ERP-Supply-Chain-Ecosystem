using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Queries;

public class ObtenerProgramacionTurnosQuery : IRequest<List<DiaProgramadoDto>>
{
    public int? EmpleadoId { get; set; }

    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }
}

public class ObtenerProgramacionTurnosQueryHandler : IRequestHandler<ObtenerProgramacionTurnosQuery, List<DiaProgramadoDto>>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerProgramacionTurnosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<List<DiaProgramadoDto>> Handle(ObtenerProgramacionTurnosQuery request, CancellationToken cancellationToken)
    {
        if (request.EmpleadoId is null || request.Desde is null || request.Hasta is null)
        {
            throw ValidacionTurno.Error400("El empleado y el rango de fechas son obligatorios.");
        }

        int idEmpleado = request.EmpleadoId.Value;
        DateOnly inicio = request.Desde.Value;
        DateOnly fin = request.Hasta.Value;
        if (fin < inicio)
        {
            throw ValidacionTurno.Error400("La fecha hasta no puede ser anterior a la fecha desde.");
        }

        if (fin.DayNumber - inicio.DayNumber + 1 > MaximoDiasProgramacion)
        {
            throw ValidacionTurno.Error400("El rango de la programación no puede superar 31 días.");
        }

        Empleado? empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == idEmpleado);
        if (empleado is null)
        {
            throw ValidacionTurno.EmpleadoInexistente(idEmpleado);
        }

        List<AsignacionTurno> asignaciones = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .Include(asignacion => asignacion.Turno)
            .Where(asignacion => asignacion.EmpleadoId == idEmpleado
                && asignacion.FechaInicio <= fin
                && (asignacion.FechaFin == null || asignacion.FechaFin >= inicio))
            .ToListAsync();
        List<DiaProgramadoDto> dias = new List<DiaProgramadoDto>();
        for (DateOnly fecha = inicio; fecha <= fin; fecha = fecha.AddDays(1))
        {
            DateOnly dia = fecha;
            AsignacionTurno? asignacionDelDia = asignaciones.FirstOrDefault(asignacion =>
                asignacion.FechaInicio <= dia && (asignacion.FechaFin == null || asignacion.FechaFin >= dia));
            dias.Add(CalculadoraTurnos.CalcularDia(asignacionDelDia?.Turno, fecha, empleado.FechaCese));
        }

        return dias;
    }

    private const int MaximoDiasProgramacion = 31;
}
