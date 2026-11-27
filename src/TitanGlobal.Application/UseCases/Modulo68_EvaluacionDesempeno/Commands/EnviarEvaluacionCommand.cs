using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class EnviarEvaluacionCommand : IRequest<EvaluacionIdDto>
{
    public int Id { get; set; }
}

public class EnviarEvaluacionCommandHandler : IRequestHandler<EnviarEvaluacionCommand, EvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public EnviarEvaluacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EvaluacionIdDto> Handle(EnviarEvaluacionCommand request, CancellationToken cancellationToken)
    {
        Evaluacion evaluacion = await OperacionesEvaluaciones.ObtenerEvaluacionAsync(_unidad, request.Id);
        OperacionesEvaluaciones.ExigirBorrador(evaluacion);
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, evaluacion.CicloEvaluacionId);
        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        ValidacionEvaluacion.ExigirVentana(ciclo);
        List<CompetenciaEvaluacion> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(competencia => competencia.CicloEvaluacionId == ciclo.Id)
            .ToListAsync();
        List<CalificacionEvaluacion> calificaciones = await _unidad.Repository<CalificacionEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(calificacion => calificacion.EvaluacionId == request.Id)
            .ToListAsync();
        int faltan = competencias.Count(competencia => !calificaciones.Any(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id));
        if (faltan > 0)
        {
            throw ValidacionEvaluacion.Error409($"Faltan calificar {faltan} competencias para enviar la evaluación.");
        }

        List<(decimal Peso, int Calificacion)> notas = competencias
            .Select(competencia => (
                competencia.Peso,
                calificaciones.First(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id).Calificacion))
            .ToList();
        evaluacion.PuntajeFinal = ValidacionEvaluacion.CalcularPuntaje(notas);
        evaluacion.Estado = ValidacionEvaluacion.EvaluacionEnviada;
        evaluacion.FechaEnvio = DateTime.UtcNow;
        await _unidad.CompleteAsync();
        return new EvaluacionIdDto { Id = evaluacion.Id };
    }
}
