using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;

// Lógica compartida por los commands y queries de AsignacionesTurno.
internal static class OperacionesAsignacionesTurno
{
    internal static async Task<AsignacionTurno> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        AsignacionTurno? asignacion = await unidad.Repository<AsignacionTurno>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (asignacion is null)
        {
            throw ValidacionTurno.AsignacionInexistente(id);
        }

        return asignacion;
    }
}
