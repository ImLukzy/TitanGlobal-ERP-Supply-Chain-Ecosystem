using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

public class ActualizarEvaluacionProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarEvaluacionProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, ActualizarEvaluacionRequestDto solicitud)
    {
        EvaluacionProveedor? evaluacion = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (evaluacion is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La evaluación {id} no existe.");
        }

        ValidacionEvaluacionProveedor.ValidarPuntajes(
            solicitud.PuntajeCumplimiento,
            solicitud.PuntajeCalidad,
            solicitud.PuntajePlazos);
        string? observaciones = ValidacionEvaluacionProveedor.NormalizarObservaciones(solicitud.Observaciones);

        decimal puntajeFinal = ValidacionEvaluacionProveedor.CalcularPuntajeFinal(
            solicitud.PuntajeCumplimiento,
            solicitud.PuntajeCalidad,
            solicitud.PuntajePlazos);
        evaluacion.PuntajeCumplimiento = solicitud.PuntajeCumplimiento;
        evaluacion.PuntajeCalidad = solicitud.PuntajeCalidad;
        evaluacion.PuntajePlazos = solicitud.PuntajePlazos;
        evaluacion.PuntajeFinal = puntajeFinal;
        evaluacion.Calificacion = ValidacionEvaluacionProveedor.CalcularCalificacion(puntajeFinal);
        evaluacion.Observaciones = observaciones;
        await _unitOfWork.CompleteAsync();
    }
}
