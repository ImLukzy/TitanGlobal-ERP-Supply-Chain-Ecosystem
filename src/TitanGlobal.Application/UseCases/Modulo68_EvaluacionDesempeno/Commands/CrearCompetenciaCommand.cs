using MediatR;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class CrearCompetenciaCommand : IRequest<CompetenciaEvaluacionIdDto>
{
    public int CicloId { get; set; }

    public required GuardarCompetenciaEvaluacionRequestDto Solicitud { get; set; }
}

public class CrearCompetenciaCommandHandler : IRequestHandler<CrearCompetenciaCommand, CompetenciaEvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearCompetenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CompetenciaEvaluacionIdDto> Handle(CrearCompetenciaCommand request, CancellationToken cancellationToken)
    {
        CompetenciaEvaluacion competencia = ValidacionEvaluacion.ValidarCompetencia(request.Solicitud);
        CicloEvaluacion ciclo = await OperacionesCiclosEvaluacion.ObtenerEnConfiguracionAsync(_unidad, request.CicloId);
        await OperacionesCiclosEvaluacion.ValidarNombreLibreAsync(_unidad, request.CicloId, competencia.Nombre, null);
        competencia.CicloEvaluacionId = ciclo.Id;
        try
        {
            await _unidad.Repository<CompetenciaEvaluacion>().AddAsync(competencia);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCompetenciaDuplicada(excepcion))
        {
            throw ValidacionEvaluacion.CompetenciaDuplicada(competencia.Nombre);
        }

        return new CompetenciaEvaluacionIdDto { Id = competencia.Id };
    }
}
