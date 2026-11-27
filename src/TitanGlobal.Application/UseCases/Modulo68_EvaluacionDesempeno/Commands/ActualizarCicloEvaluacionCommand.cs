using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class ActualizarCicloEvaluacionCommand : IRequest<CicloEvaluacionIdDto>
{
    public int Id { get; set; }

    public required GuardarCicloEvaluacionRequestDto Solicitud { get; set; }
}

public class ActualizarCicloEvaluacionCommandHandler : IRequestHandler<ActualizarCicloEvaluacionCommand, CicloEvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarCicloEvaluacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CicloEvaluacionIdDto> Handle(ActualizarCicloEvaluacionCommand request, CancellationToken cancellationToken)
    {
        CicloEvaluacion nuevo = ValidacionEvaluacion.ValidarCiclo(request.Solicitud);
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, request.Id);
        if (ciclo.Estado != ValidacionEvaluacion.CicloConfiguracion)
        {
            throw ValidacionEvaluacion.CicloNoEnConfiguracion(ciclo.Codigo);
        }

        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<CicloEvaluacion>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != request.Id);
        if (existe)
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        ciclo.Codigo = nuevo.Codigo;
        ciclo.Nombre = nuevo.Nombre;
        ciclo.Periodo = nuevo.Periodo;
        ciclo.FechaInicioPeriodo = nuevo.FechaInicioPeriodo;
        ciclo.FechaFinPeriodo = nuevo.FechaFinPeriodo;
        ciclo.FechaApertura = nuevo.FechaApertura;
        ciclo.FechaCierre = nuevo.FechaCierre;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }
}
