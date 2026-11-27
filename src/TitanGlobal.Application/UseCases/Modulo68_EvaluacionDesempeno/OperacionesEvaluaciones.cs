using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno;

// Lógica compartida por los commands y queries de Evaluaciones.
internal static class OperacionesEvaluaciones
{
    internal static async Task<Evaluacion> ObtenerEvaluacionAsync(IUnitOfWork unidad, int id)
    {
        Evaluacion? evaluacion = await unidad.Repository<Evaluacion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (evaluacion is null)
        {
            throw ValidacionEvaluacion.EvaluacionInexistente(id);
        }

        return evaluacion;
    }

    internal static void ExigirBorrador(Evaluacion evaluacion)
    {
        if (evaluacion.Estado != ValidacionEvaluacion.EvaluacionBorrador)
        {
            throw ValidacionEvaluacion.Error409($"La evaluación {evaluacion.Id} no está en borrador.");
        }
    }
}
