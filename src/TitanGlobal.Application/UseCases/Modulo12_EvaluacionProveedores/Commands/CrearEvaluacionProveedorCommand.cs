using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores.Commands;

public class CrearEvaluacionProveedorCommand : IRequest<int>
{
    public required CrearEvaluacionRequestDto Solicitud { get; set; }
}

public class CrearEvaluacionProveedorCommandHandler : IRequestHandler<CrearEvaluacionProveedorCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearEvaluacionProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearEvaluacionProveedorCommand request, CancellationToken cancellationToken)
    {
        ValidacionEvaluacionProveedor.ValidarProveedor(request.Solicitud.ProveedorId);
        string periodo = ValidacionEvaluacionProveedor.NormalizarPeriodo(request.Solicitud.Periodo);
        ValidacionEvaluacionProveedor.ValidarPeriodoNoFuturo(periodo);
        ValidacionEvaluacionProveedor.ValidarPuntajes(
            request.Solicitud.PuntajeCumplimiento,
            request.Solicitud.PuntajeCalidad,
            request.Solicitud.PuntajePlazos);
        string? observaciones = ValidacionEvaluacionProveedor.NormalizarObservaciones(request.Solicitud.Observaciones);

        bool proveedorExiste = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .AnyAsync(proveedor => proveedor.Id == request.Solicitud.ProveedorId);
        if (!proveedorExiste)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El proveedor {request.Solicitud.ProveedorId} no existe.");
        }

        bool periodoRepetido = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .AnyAsync(evaluacion => evaluacion.ProveedorId == request.Solicitud.ProveedorId && evaluacion.Periodo == periodo);
        if (periodoRepetido)
        {
            throw CrearConflictoPeriodoException(request.Solicitud.ProveedorId, periodo);
        }

        decimal puntajeFinal = ValidacionEvaluacionProveedor.CalcularPuntajeFinal(
            request.Solicitud.PuntajeCumplimiento,
            request.Solicitud.PuntajeCalidad,
            request.Solicitud.PuntajePlazos);
        EvaluacionProveedor evaluacionNueva = new EvaluacionProveedor
        {
            ProveedorId = request.Solicitud.ProveedorId,
            Periodo = periodo,
            PuntajeCumplimiento = request.Solicitud.PuntajeCumplimiento,
            PuntajeCalidad = request.Solicitud.PuntajeCalidad,
            PuntajePlazos = request.Solicitud.PuntajePlazos,
            PuntajeFinal = puntajeFinal,
            Calificacion = ValidacionEvaluacionProveedor.CalcularCalificacion(puntajeFinal),
            Observaciones = observaciones,
            FechaRegistro = DateTime.UtcNow
        };
        await _unitOfWork.Repository<EvaluacionProveedor>().AddAsync(evaluacionNueva);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoPeriodoException(request.Solicitud.ProveedorId, periodo);
        }

        return evaluacionNueva.Id;
    }

    private static ReglaNegocioException CrearConflictoPeriodoException(int proveedorId, string periodo)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"El proveedor {proveedorId} ya tiene una evaluación del periodo {periodo}.");
    }
}
