using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

public class CrearEvaluacionProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearEvaluacionProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearEvaluacionRequestDto solicitud)
    {
        ValidacionEvaluacionProveedor.ValidarProveedor(solicitud.ProveedorId);
        string periodo = ValidacionEvaluacionProveedor.NormalizarPeriodo(solicitud.Periodo);
        ValidacionEvaluacionProveedor.ValidarPeriodoNoFuturo(periodo);
        ValidacionEvaluacionProveedor.ValidarPuntajes(
            solicitud.PuntajeCumplimiento,
            solicitud.PuntajeCalidad,
            solicitud.PuntajePlazos);
        string? observaciones = ValidacionEvaluacionProveedor.NormalizarObservaciones(solicitud.Observaciones);

        bool proveedorExiste = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .AnyAsync(proveedor => proveedor.Id == solicitud.ProveedorId);
        if (!proveedorExiste)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El proveedor {solicitud.ProveedorId} no existe.");
        }

        bool periodoRepetido = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .AnyAsync(evaluacion => evaluacion.ProveedorId == solicitud.ProveedorId && evaluacion.Periodo == periodo);
        if (periodoRepetido)
        {
            throw CrearConflictoPeriodoException(solicitud.ProveedorId, periodo);
        }

        decimal puntajeFinal = ValidacionEvaluacionProveedor.CalcularPuntajeFinal(
            solicitud.PuntajeCumplimiento,
            solicitud.PuntajeCalidad,
            solicitud.PuntajePlazos);
        EvaluacionProveedor evaluacionNueva = new EvaluacionProveedor
        {
            ProveedorId = solicitud.ProveedorId,
            Periodo = periodo,
            PuntajeCumplimiento = solicitud.PuntajeCumplimiento,
            PuntajeCalidad = solicitud.PuntajeCalidad,
            PuntajePlazos = solicitud.PuntajePlazos,
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
            throw CrearConflictoPeriodoException(solicitud.ProveedorId, periodo);
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
