using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores.Queries;

public class ObtenerEvaluacionProveedorQuery : IRequest<EvaluacionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerEvaluacionProveedorQueryHandler : IRequestHandler<ObtenerEvaluacionProveedorQuery, EvaluacionDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerEvaluacionProveedorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EvaluacionDetalleDto> Handle(ObtenerEvaluacionProveedorQuery request, CancellationToken cancellationToken)
    {
        EvaluacionDetalleDto? evaluacion = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.Id)
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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La evaluación {request.Id} no existe.");
        }

        return evaluacion;
    }
}
