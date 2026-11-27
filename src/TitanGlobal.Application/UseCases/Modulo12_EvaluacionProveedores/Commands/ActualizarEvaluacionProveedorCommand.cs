using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores.Commands;

public class ActualizarEvaluacionProveedorCommand : IRequest<int>
{
    public int Id { get; set; }

    public required ActualizarEvaluacionRequestDto Solicitud { get; set; }
}

public class ActualizarEvaluacionProveedorCommandHandler : IRequestHandler<ActualizarEvaluacionProveedorCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarEvaluacionProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarEvaluacionProveedorCommand request, CancellationToken cancellationToken)
    {
        EvaluacionProveedor? evaluacion = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (evaluacion is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La evaluación {request.Id} no existe.");
        }

        ValidacionEvaluacionProveedor.ValidarPuntajes(
            request.Solicitud.PuntajeCumplimiento,
            request.Solicitud.PuntajeCalidad,
            request.Solicitud.PuntajePlazos);
        string? observaciones = ValidacionEvaluacionProveedor.NormalizarObservaciones(request.Solicitud.Observaciones);

        decimal puntajeFinal = ValidacionEvaluacionProveedor.CalcularPuntajeFinal(
            request.Solicitud.PuntajeCumplimiento,
            request.Solicitud.PuntajeCalidad,
            request.Solicitud.PuntajePlazos);
        evaluacion.PuntajeCumplimiento = request.Solicitud.PuntajeCumplimiento;
        evaluacion.PuntajeCalidad = request.Solicitud.PuntajeCalidad;
        evaluacion.PuntajePlazos = request.Solicitud.PuntajePlazos;
        evaluacion.PuntajeFinal = puntajeFinal;
        evaluacion.Calificacion = ValidacionEvaluacionProveedor.CalcularCalificacion(puntajeFinal);
        evaluacion.Observaciones = observaciones;
        await _unitOfWork.CompleteAsync();

        return evaluacion.Id;
    }
}
