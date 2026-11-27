using MediatR;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class ActualizarCompetenciaCommand : IRequest<CompetenciaEvaluacionIdDto>
{
    public int CicloId { get; set; }

    public int CompetenciaId { get; set; }

    public required GuardarCompetenciaEvaluacionRequestDto Solicitud { get; set; }
}

public class ActualizarCompetenciaCommandHandler : IRequestHandler<ActualizarCompetenciaCommand, CompetenciaEvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarCompetenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CompetenciaEvaluacionIdDto> Handle(ActualizarCompetenciaCommand request, CancellationToken cancellationToken)
    {
        CompetenciaEvaluacion nueva = ValidacionEvaluacion.ValidarCompetencia(request.Solicitud);
        await OperacionesCiclosEvaluacion.ObtenerEnConfiguracionAsync(_unidad, request.CicloId);
        CompetenciaEvaluacion competencia = await OperacionesCiclosEvaluacion.ObtenerCompetenciaAsync(_unidad, request.CicloId, request.CompetenciaId);
        await OperacionesCiclosEvaluacion.ValidarNombreLibreAsync(_unidad, request.CicloId, nueva.Nombre, request.CompetenciaId);
        competencia.Nombre = nueva.Nombre;
        competencia.Descripcion = nueva.Descripcion;
        competencia.Peso = nueva.Peso;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCompetenciaDuplicada(excepcion))
        {
            throw ValidacionEvaluacion.CompetenciaDuplicada(nueva.Nombre);
        }

        return new CompetenciaEvaluacionIdDto { Id = competencia.Id };
    }
}
