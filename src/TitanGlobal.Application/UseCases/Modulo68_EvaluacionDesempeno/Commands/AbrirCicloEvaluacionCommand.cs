using MediatR;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class AbrirCicloEvaluacionCommand : IRequest<CicloEvaluacionIdDto>
{
    public int Id { get; set; }
}

public class AbrirCicloEvaluacionCommandHandler : IRequestHandler<AbrirCicloEvaluacionCommand, CicloEvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AbrirCicloEvaluacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CicloEvaluacionIdDto> Handle(AbrirCicloEvaluacionCommand request, CancellationToken cancellationToken)
    {
        CicloEvaluacion ciclo = await OperacionesCiclosEvaluacion.ObtenerEnConfiguracionAsync(_unidad, request.Id);
        decimal suma = await ValidacionEvaluacion.SumarPesosAsync(_unidad, request.Id);
        if (suma != ValidacionEvaluacion.PesoTotal)
        {
            throw ValidacionEvaluacion.Error409(
                $"Los pesos de las competencias suman {ValidacionEvaluacion.Decimales(suma)} y deben sumar 100.");
        }

        ciclo.Estado = ValidacionEvaluacion.CicloAbierto;
        await _unidad.CompleteAsync();
        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }
}
