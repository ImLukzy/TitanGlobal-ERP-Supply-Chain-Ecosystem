using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Commands;

public class CrearEvaluacionCommand : IRequest<EvaluacionIdDto>
{
    public required CrearEvaluacionRequestDto Solicitud { get; set; }
}

public class CrearEvaluacionCommandHandler : IRequestHandler<CrearEvaluacionCommand, EvaluacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearEvaluacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EvaluacionIdDto> Handle(CrearEvaluacionCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.CicloId is null || request.Solicitud.EmpleadoId is null || request.Solicitud.EvaluadorId is null)
        {
            throw ValidacionEvaluacion.Error400("El ciclo, el empleado y el evaluador son obligatorios.");
        }

        int cicloId = request.Solicitud.CicloId.Value;
        int empleadoId = request.Solicitud.EmpleadoId.Value;
        int evaluadorId = request.Solicitud.EvaluadorId.Value;
        if (empleadoId == evaluadorId)
        {
            throw ValidacionEvaluacion.Error400("El evaluador no puede ser el propio evaluado.");
        }

        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, cicloId);
        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        ValidacionEvaluacion.ExigirVentana(ciclo);
        await ValidacionEvaluacion.ObtenerEmpleadoAsync(_unidad, empleadoId);
        bool conContrato = await ValidacionEvaluacion.TuvoContratoEnPeriodoAsync(
            _unidad,
            empleadoId,
            ciclo.FechaInicioPeriodo,
            ciclo.FechaFinPeriodo);
        if (!conContrato)
        {
            throw ValidacionEvaluacion.Error409($"El empleado {empleadoId} no tuvo contrato vigente durante el periodo del ciclo.");
        }

        Empleado evaluador = await ValidacionEvaluacion.ObtenerEmpleadoAsync(_unidad, evaluadorId);
        if (evaluador.Estado != ValidacionEvaluacion.EmpleadoActivo)
        {
            throw ValidacionEvaluacion.Error409($"El evaluador {evaluadorId} no está activo.");
        }

        bool existe = await _unidad.Repository<Evaluacion>().AsQueryable()
            .AnyAsync(otra => otra.CicloEvaluacionId == cicloId && otra.EmpleadoId == empleadoId);
        if (existe)
        {
            throw ValidacionEvaluacion.EvaluacionDuplicada(empleadoId, ciclo.Codigo);
        }

        Evaluacion evaluacion = new Evaluacion
        {
            CicloEvaluacionId = cicloId,
            EmpleadoId = empleadoId,
            EvaluadorId = evaluadorId,
            Estado = ValidacionEvaluacion.EvaluacionBorrador
        };
        try
        {
            await _unidad.Repository<Evaluacion>().AddAsync(evaluacion);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsEvaluacionDuplicada(excepcion))
        {
            throw ValidacionEvaluacion.EvaluacionDuplicada(empleadoId, ciclo.Codigo);
        }

        return new EvaluacionIdDto { Id = evaluacion.Id };
    }
}
