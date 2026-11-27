using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

// Lógica compartida por los commands y queries de Capacitaciones.
internal static class OperacionesCapacitaciones
{
    // K4: una certificación está vencida cuando su vencimiento es anterior a hoy; sin vencimiento no vence.
    internal static bool EstaVencida(DateOnly? fechaVencimiento, DateOnly hoy)
    {
        return fechaVencimiento is not null && fechaVencimiento.Value < hoy;
    }

    internal static async Task<List<InscripcionCurso>> ConsultarAprobadasAsync(IUnitOfWork unidad, int anio, List<int>? empleadoIds)
    {
        DateOnly inicioAnio = new DateOnly(anio, 1, 1);
        DateOnly finAnio = new DateOnly(anio, 12, 31);
        IQueryable<InscripcionCurso> consulta = unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.ProgramacionCurso)
            .ThenInclude(programacion => programacion.Curso)
            .AsNoTracking()
            .Where(inscripcion => inscripcion.Estado == ValidacionCapacitacion.InscripcionAprobado
                && inscripcion.ProgramacionCurso.FechaFin >= inicioAnio
                && inscripcion.ProgramacionCurso.FechaFin <= finAnio);
        if (empleadoIds is not null)
        {
            consulta = consulta.Where(inscripcion => empleadoIds.Contains(inscripcion.EmpleadoId));
        }

        return await consulta.ToListAsync();
    }
}
