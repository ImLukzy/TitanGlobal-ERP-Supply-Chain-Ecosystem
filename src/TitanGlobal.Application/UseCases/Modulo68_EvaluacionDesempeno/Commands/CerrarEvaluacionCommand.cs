using MediatR;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class CerrarEvaluacionCommand : IRequest<EvaluacionIdDto>
{
    public int Id { get; set; }
}

public class CerrarEvaluacionCommandHandler : IRequestHandler<CerrarEvaluacionCommand, EvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CerrarEvaluacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EvaluacionIdDto> Handle(CerrarEvaluacionCommand request, CancellationToken cancellationToken)
    {
        Evaluacion evaluacion = await OperacionesEvaluaciones.ObtenerEvaluacionAsync(_unidad, request.Id);
        if (evaluacion.Estado != ValidacionEvaluacion.EvaluacionEnviada)
        {
            throw ValidacionEvaluacion.Error409($"La evaluación {request.Id} debe estar enviada para cerrarse.");
        }

        evaluacion.Estado = ValidacionEvaluacion.EvaluacionCerrada;
        evaluacion.FechaCierre = DateTime.UtcNow;
        await _unidad.CompleteAsync();
        return new EvaluacionIdDto { Id = evaluacion.Id };
    }
}
