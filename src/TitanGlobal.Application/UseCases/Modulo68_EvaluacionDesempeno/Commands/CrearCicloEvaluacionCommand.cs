using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class CrearCicloEvaluacionCommand : IRequest<CicloEvaluacionIdDto>
{
    public required GuardarCicloEvaluacionRequestDto Solicitud { get; set; }
}

public class CrearCicloEvaluacionCommandHandler : IRequestHandler<CrearCicloEvaluacionCommand, CicloEvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearCicloEvaluacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CicloEvaluacionIdDto> Handle(CrearCicloEvaluacionCommand request, CancellationToken cancellationToken)
    {
        CicloEvaluacion ciclo = ValidacionEvaluacion.ValidarCiclo(request.Solicitud);
        string codigo = ciclo.Codigo;
        bool existe = await _unidad.Repository<CicloEvaluacion>().AsQueryable().AnyAsync(otro => otro.Codigo == codigo);
        if (existe)
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        try
        {
            await _unidad.Repository<CicloEvaluacion>().AddAsync(ciclo);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }
}
