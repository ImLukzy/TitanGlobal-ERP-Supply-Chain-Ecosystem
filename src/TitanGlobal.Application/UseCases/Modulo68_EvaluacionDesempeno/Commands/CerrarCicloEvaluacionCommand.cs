using MediatR;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class CerrarCicloEvaluacionCommand : IRequest<CicloEvaluacionIdDto>
{
    public int Id { get; set; }
}

public class CerrarCicloEvaluacionCommandHandler : IRequestHandler<CerrarCicloEvaluacionCommand, CicloEvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CerrarCicloEvaluacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // Cierra el ciclo y sus evaluaciones enviadas; las que siguen en borrador quedan congeladas.
    public async Task<CicloEvaluacionIdDto> Handle(CerrarCicloEvaluacionCommand request, CancellationToken cancellationToken)
    {
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, request.Id);
        if (ciclo.Estado == ValidacionEvaluacion.CicloCerrado)
        {
            throw ValidacionEvaluacion.Error409($"El ciclo {ciclo.Codigo} ya está cerrado.");
        }

        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        List<Evaluacion> enviadas = await _unidad.Repository<Evaluacion>().AsQueryable()
            .Where(evaluacion => evaluacion.CicloEvaluacionId == request.Id
                && evaluacion.Estado == ValidacionEvaluacion.EvaluacionEnviada)
            .ToListAsync();
        DateTime ahora = DateTime.UtcNow;
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        foreach (Evaluacion evaluacion in enviadas)
        {
            evaluacion.Estado = ValidacionEvaluacion.EvaluacionCerrada;
            evaluacion.FechaCierre = ahora;
        }

        ciclo.Estado = ValidacionEvaluacion.CicloCerrado;
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }
}
