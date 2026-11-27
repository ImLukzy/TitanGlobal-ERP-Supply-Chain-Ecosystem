using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Queries;

public class ObtenerTurnoQuery : IRequest<TurnoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerTurnoQueryHandler : IRequestHandler<ObtenerTurnoQuery, TurnoDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerTurnoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<TurnoDetalleDto> Handle(ObtenerTurnoQuery request, CancellationToken cancellationToken)
    {
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, request.Id);
        DateOnly hoy = ValidacionTurno.Hoy();
        IQueryable<AsignacionTurno> asignaciones = _unidad.Repository<AsignacionTurno>().AsQueryable()
            .Where(asignacion => asignacion.TurnoId == request.Id);
        int totalAsignaciones = await asignaciones.CountAsync();
        int conTurnoHoy = await asignaciones
            .Where(asignacion => asignacion.FechaInicio <= hoy
                && (asignacion.FechaFin == null || asignacion.FechaFin >= hoy)
                && asignacion.Empleado.Estado != ValidacionTurno.EstadoEmpleadoCesado)
            .Select(asignacion => asignacion.EmpleadoId)
            .Distinct()
            .CountAsync();
        TurnoDetalleDto detalle = new TurnoDetalleDto
        {
            TotalAsignaciones = totalAsignaciones,
            EmpleadosConTurnoHoy = conTurnoHoy
        };
        ValidacionTurno.CopiarEn(turno, detalle);
        return detalle;
    }
}
