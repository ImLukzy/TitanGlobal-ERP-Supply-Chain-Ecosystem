using MediatR;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class GuardarCalificacionesCommand : IRequest<EvaluacionIdDto>
{
    public int Id { get; set; }

    public required GuardarCalificacionesRequestDto Solicitud { get; set; }
}

public class GuardarCalificacionesCommandHandler : IRequestHandler<GuardarCalificacionesCommand, EvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public GuardarCalificacionesCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // Reemplaza las calificaciones existentes de una evaluación en borrador.
    public async Task<EvaluacionIdDto> Handle(GuardarCalificacionesCommand request, CancellationToken cancellationToken)
    {
        Evaluacion evaluacion = await OperacionesEvaluaciones.ObtenerEvaluacionAsync(_unidad, request.Id);
        OperacionesEvaluaciones.ExigirBorrador(evaluacion);
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, evaluacion.CicloEvaluacionId);
        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        string comentario = request.Solicitud.Comentario?.Trim() ?? string.Empty;
        if (comentario.Length > ValidacionEvaluacion.MaximoComentario)
        {
            throw ValidacionEvaluacion.Error400("El comentario no puede superar 500 caracteres.");
        }

        List<int> idsCompetencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .Where(competencia => competencia.CicloEvaluacionId == ciclo.Id)
            .Select(competencia => competencia.Id)
            .ToListAsync();
        Dictionary<int, int> nuevas = new Dictionary<int, int>();
        foreach (CalificacionRequestDto entrada in request.Solicitud.Calificaciones ?? new List<CalificacionRequestDto>())
        {
            decimal? nota = entrada.Calificacion;
            if (nota is null || nota.Value < 1m || nota.Value > 5m || nota.Value != decimal.Truncate(nota.Value))
            {
                throw ValidacionEvaluacion.Error400("La calificación debe ser un número entero de 1 a 5.");
            }

            int competenciaId = entrada.CompetenciaId ?? 0;
            if (!idsCompetencias.Contains(competenciaId))
            {
                throw ValidacionEvaluacion.Error400($"La competencia {competenciaId} no pertenece al ciclo de la evaluación.");
            }

            nuevas[competenciaId] = (int)nota.Value;
        }

        List<CalificacionEvaluacion> anteriores = await _unidad.Repository<CalificacionEvaluacion>().AsQueryable()
            .Where(calificacion => calificacion.EvaluacionId == request.Id)
            .ToListAsync();
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        foreach (CalificacionEvaluacion anterior in anteriores)
        {
            _unidad.Repository<CalificacionEvaluacion>().Remove(anterior);
        }

        await _unidad.CompleteAsync();
        foreach (KeyValuePair<int, int> nueva in nuevas)
        {
            await _unidad.Repository<CalificacionEvaluacion>().AddAsync(new CalificacionEvaluacion
            {
                EvaluacionId = request.Id,
                CompetenciaEvaluacionId = nueva.Key,
                Calificacion = nueva.Value
            });
        }

        evaluacion.Comentario = comentario.Length == 0 ? null : comentario;
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new EvaluacionIdDto { Id = evaluacion.Id };
    }
}
