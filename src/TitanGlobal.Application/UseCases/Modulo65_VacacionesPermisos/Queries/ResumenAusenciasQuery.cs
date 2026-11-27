using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Queries;

public class ResumenAusenciasQuery : IRequest<ResumenAusenciasDto>
{
}

public class ResumenAusenciasQueryHandler : IRequestHandler<ResumenAusenciasQuery, ResumenAusenciasDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenAusenciasQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenAusenciasDto> Handle(ResumenAusenciasQuery request, CancellationToken cancellationToken)
    {
        DateOnly hoy = ValidacionAusencia.Hoy();
        DateOnly inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        DateOnly finMes = inicioMes.AddMonths(1).AddDays(-1);
        List<FilaResumen> filas = await _unidad.Repository<Ausencia>().AsQueryable()
            .Where(ausencia => ausencia.Estado == ValidacionAusencia.EstadoPendiente
                || ausencia.Estado == ValidacionAusencia.EstadoAprobada)
            .Select(ausencia => new FilaResumen
            {
                Tipo = ausencia.Tipo,
                Estado = ausencia.Estado,
                FechaInicio = ausencia.FechaInicio,
                FechaFin = ausencia.FechaFin
            })
            .ToListAsync();
        List<FilaResumen> aprobadas = filas.Where(fila => fila.Estado == ValidacionAusencia.EstadoAprobada).ToList();
        List<FilaResumen> deHoy = aprobadas.Where(fila => fila.FechaInicio <= hoy && fila.FechaFin >= hoy).ToList();
        return new ResumenAusenciasDto
        {
            Pendientes = filas.Count(fila => fila.Estado == ValidacionAusencia.EstadoPendiente),
            AusentesHoy = deHoy.Count,
            VacacionesHoy = deHoy.Count(fila => fila.Tipo == ValidacionAusencia.TipoVacaciones),
            DescansosMedicosHoy = deHoy.Count(fila => fila.Tipo == ValidacionAusencia.TipoDescansoMedico),
            AprobadasMes = aprobadas.Count(fila => fila.FechaInicio >= inicioMes && fila.FechaInicio <= finMes)
        };
    }

    private sealed class FilaResumen
    {
        public string Tipo { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public DateOnly FechaInicio { get; set; }

        public DateOnly FechaFin { get; set; }
    }
}
