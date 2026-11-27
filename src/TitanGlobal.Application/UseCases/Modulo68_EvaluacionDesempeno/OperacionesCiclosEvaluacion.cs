using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno;

// Lógica compartida por los commands y queries de CiclosEvaluacion.
internal static class OperacionesCiclosEvaluacion
{
    internal static async Task<CicloEvaluacion> ObtenerSinSeguimientoAsync(IUnitOfWork unidad, int id)
    {
        CicloEvaluacion? ciclo = await unidad.Repository<CicloEvaluacion>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (ciclo is null)
        {
            throw ValidacionEvaluacion.CicloInexistente(id);
        }

        return ciclo;
    }

    internal static async Task<CicloEvaluacion> ObtenerEnConfiguracionAsync(IUnitOfWork unidad, int id)
    {
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(unidad, id);
        if (ciclo.Estado != ValidacionEvaluacion.CicloConfiguracion)
        {
            throw ValidacionEvaluacion.CicloNoEnConfiguracion(ciclo.Codigo);
        }

        return ciclo;
    }

    internal static async Task<CompetenciaEvaluacion> ObtenerCompetenciaAsync(IUnitOfWork unidad, int cicloId, int competenciaId)
    {
        CompetenciaEvaluacion? competencia = await unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == competenciaId && entidad.CicloEvaluacionId == cicloId);
        if (competencia is null)
        {
            throw ValidacionEvaluacion.Error404($"La competencia {competenciaId} no existe en el ciclo.");
        }

        return competencia;
    }

    internal static async Task ValidarNombreLibreAsync(IUnitOfWork unidad, int cicloId, string nombre, int? excluirId)
    {
        string minuscula = nombre.ToLowerInvariant();
        bool existe = await unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AnyAsync(otra => otra.CicloEvaluacionId == cicloId
                && otra.Nombre.ToLower() == minuscula
                && (excluirId == null || otra.Id != excluirId.Value));
        if (existe)
        {
            throw ValidacionEvaluacion.CompetenciaDuplicada(nombre);
        }
    }
}
