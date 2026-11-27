using MediatR;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class EliminarCompetenciaCommand : IRequest
{
    public int CicloId { get; set; }

    public int CompetenciaId { get; set; }
}

public class EliminarCompetenciaCommandHandler : IRequestHandler<EliminarCompetenciaCommand>
{
    private readonly IUnitOfWork _unidad;

    public EliminarCompetenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task Handle(EliminarCompetenciaCommand request, CancellationToken cancellationToken)
    {
        await OperacionesCiclosEvaluacion.ObtenerEnConfiguracionAsync(_unidad, request.CicloId);
        CompetenciaEvaluacion competencia = await OperacionesCiclosEvaluacion.ObtenerCompetenciaAsync(_unidad, request.CicloId, request.CompetenciaId);
        _unidad.Repository<CompetenciaEvaluacion>().Remove(competencia);
        await _unidad.CompleteAsync();
    }
}
