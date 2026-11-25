using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

public class ObtenerEvaluacionProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerEvaluacionProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EvaluacionDetalleDto> ExecuteAsync(int id)
    {
        EvaluacionDetalleDto? evaluacion = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new EvaluacionDetalleDto
            {
                Id = entidad.Id,
                ProveedorId = entidad.ProveedorId,
                ProveedorNombre = entidad.Proveedor.RazonSocial,
                Periodo = entidad.Periodo,
                PuntajeCumplimiento = entidad.PuntajeCumplimiento,
                PuntajeCalidad = entidad.PuntajeCalidad,
                PuntajePlazos = entidad.PuntajePlazos,
                PuntajeFinal = entidad.PuntajeFinal,
                Calificacion = entidad.Calificacion,
                Observaciones = entidad.Observaciones,
                FechaRegistro = entidad.FechaRegistro
            })
            .SingleOrDefaultAsync();

        if (evaluacion is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La evaluación {id} no existe.");
        }

        return evaluacion;
    }
}
