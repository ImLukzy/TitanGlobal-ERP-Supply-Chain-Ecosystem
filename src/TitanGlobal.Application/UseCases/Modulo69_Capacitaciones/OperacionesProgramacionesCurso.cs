using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

// Lógica compartida por los commands y queries de ProgramacionesCurso.
internal static class OperacionesProgramacionesCurso
{
    internal static async Task<InscripcionCurso> ObtenerInscripcionAsync(IUnitOfWork unidad, int programacionId, int inscripcionId)
    {
        InscripcionCurso? inscripcion = await unidad.Repository<InscripcionCurso>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == inscripcionId && entidad.ProgramacionCursoId == programacionId);
        if (inscripcion is null)
        {
            throw ValidacionCapacitacion.InscripcionInexistente(inscripcionId);
        }

        return inscripcion;
    }
}
