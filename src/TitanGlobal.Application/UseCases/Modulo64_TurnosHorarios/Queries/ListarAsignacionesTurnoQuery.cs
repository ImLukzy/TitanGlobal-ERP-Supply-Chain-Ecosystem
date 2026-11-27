using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Queries;

public class ListarAsignacionesTurnoQuery : IRequest<PaginaAsignacionesTurnoDto>
{
    public int? EmpleadoId { get; set; }

    public int? TurnoId { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarAsignacionesTurnoQueryHandler : IRequestHandler<ListarAsignacionesTurnoQuery, PaginaAsignacionesTurnoDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarAsignacionesTurnoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaAsignacionesTurnoDto> Handle(ListarAsignacionesTurnoQuery request, CancellationToken cancellationToken)
    {
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionTurno.ValidarListadoAsignaciones(request.Pagina, request.TamanoPagina, estadoNormalizado);
        DateOnly hoy = ValidacionTurno.Hoy();
        IQueryable<AsignacionTurno> consulta = _unidad.Repository<AsignacionTurno>().AsQueryable();
        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(asignacion => asignacion.EmpleadoId == idEmpleado);
        }

        if (request.TurnoId is not null)
        {
            int idTurno = request.TurnoId.Value;
            consulta = consulta.Where(asignacion => asignacion.TurnoId == idTurno);
        }

        consulta = AplicarFiltroEstado(consulta, estadoNormalizado, hoy);
        int total = await consulta.CountAsync();
        List<AsignacionTurnoListadoDto> items = await consulta
            .OrderByDescending(asignacion => asignacion.FechaInicio)
            .ThenByDescending(asignacion => asignacion.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(asignacion => new AsignacionTurnoListadoDto
            {
                Id = asignacion.Id,
                EmpleadoId = asignacion.EmpleadoId,
                EmpleadoLegajo = asignacion.Empleado.Legajo,
                EmpleadoNombre = asignacion.Empleado.Apellidos + ", " + asignacion.Empleado.Nombres,
                TurnoId = asignacion.TurnoId,
                TurnoCodigo = asignacion.Turno.Codigo,
                TurnoNombre = asignacion.Turno.Nombre,
                FechaInicio = asignacion.FechaInicio,
                FechaFin = asignacion.FechaFin,
                Estado = asignacion.FechaInicio > hoy
                    ? ValidacionTurno.EstadoFutura
                    : (asignacion.FechaFin != null && asignacion.FechaFin < hoy
                        ? ValidacionTurno.EstadoFinalizada
                        : ValidacionTurno.EstadoVigente)
            })
            .ToListAsync();
        return new PaginaAsignacionesTurnoDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static IQueryable<AsignacionTurno> AplicarFiltroEstado(
        IQueryable<AsignacionTurno> consulta,
        string estado,
        DateOnly hoy)
    {
        if (estado == ValidacionTurno.EstadoFutura)
        {
            return consulta.Where(asignacion => asignacion.FechaInicio > hoy);
        }

        if (estado == ValidacionTurno.EstadoFinalizada)
        {
            return consulta.Where(asignacion => asignacion.FechaInicio <= hoy
                && asignacion.FechaFin != null
                && asignacion.FechaFin < hoy);
        }

        if (estado == ValidacionTurno.EstadoVigente)
        {
            return consulta.Where(asignacion => asignacion.FechaInicio <= hoy
                && (asignacion.FechaFin == null || asignacion.FechaFin >= hoy));
        }

        return consulta;
    }
}
