using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Queries;

public class ObtenerResultadosQuery : IRequest<ResultadosCicloEvaluacionDto>
{
    public int Id { get; set; }
}

public class ObtenerResultadosQueryHandler : IRequestHandler<ObtenerResultadosQuery, ResultadosCicloEvaluacionDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerResultadosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResultadosCicloEvaluacionDto> Handle(ObtenerResultadosQuery request, CancellationToken cancellationToken)
    {
        await OperacionesCiclosEvaluacion.ObtenerSinSeguimientoAsync(_unidad, request.Id);
        List<CompetenciaEvaluacion> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(competencia => competencia.CicloEvaluacionId == request.Id)
            .OrderBy(competencia => competencia.Id)
            .ToListAsync();
        List<Evaluacion> evaluaciones = await _unidad.Repository<Evaluacion>().AsQueryable()
            .AsNoTracking()
            .Include(evaluacion => evaluacion.CalificacionesEvaluacion)
            .Where(evaluacion => evaluacion.CicloEvaluacionId == request.Id
                && (evaluacion.Estado == ValidacionEvaluacion.EvaluacionEnviada
                    || evaluacion.Estado == ValidacionEvaluacion.EvaluacionCerrada))
            .ToListAsync();
        List<decimal> puntajes = evaluaciones.Select(evaluacion => evaluacion.PuntajeFinal ?? 0m).ToList();
        List<DistribucionPuntajeDto> distribucion = ValidacionEvaluacion.Etiquetas
            .Select(etiqueta => new DistribucionPuntajeDto
            {
                Etiqueta = etiqueta,
                Cantidad = puntajes.Count(puntaje => ValidacionEvaluacion.Etiqueta(puntaje) == etiqueta)
            })
            .ToList();
        List<PromedioCompetenciaDto> porCompetencia = new List<PromedioCompetenciaDto>();
        foreach (CompetenciaEvaluacion competencia in competencias)
        {
            List<int> notas = evaluaciones
                .SelectMany(evaluacion => evaluacion.CalificacionesEvaluacion)
                .Where(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id)
                .Select(calificacion => calificacion.Calificacion)
                .ToList();
            decimal promedio = notas.Count == 0 ? 0m : ValidacionEvaluacion.Redondear((decimal)notas.Sum() / notas.Count);
            porCompetencia.Add(new PromedioCompetenciaDto { Competencia = competencia.Nombre, Promedio = promedio });
        }

        return new ResultadosCicloEvaluacionDto
        {
            Evaluadas = evaluaciones.Count,
            PuntajePromedio = puntajes.Count == 0 ? 0m : ValidacionEvaluacion.Redondear(puntajes.Sum() / puntajes.Count),
            Distribucion = distribucion,
            PorCompetencia = porCompetencia
        };
    }
}
