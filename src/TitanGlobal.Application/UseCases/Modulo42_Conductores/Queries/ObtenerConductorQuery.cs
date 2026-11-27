using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores.Queries;

public class ObtenerConductorQuery : IRequest<ConductorDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerConductorQueryHandler : IRequestHandler<ObtenerConductorQuery, ConductorDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerConductorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConductorDetalleDto> Handle(ObtenerConductorQuery request, CancellationToken cancellationToken)
    {
        ConductorDetalleDto? conductor = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .Where(conductor => conductor.Id == request.Id)
            .Select(conductor => new ConductorDetalleDto
            {
                Id = conductor.Id,
                Nombre = conductor.Nombre,
                DocumentoLicencia = conductor.DocumentoLicencia,
                VencimientoLicencia = conductor.VencimientoLicencia,
                Habilitado = conductor.Habilitado,
                Activo = conductor.Activo,
                Observaciones = conductor.Observaciones
            })
            .FirstOrDefaultAsync();

        if (conductor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El conductor {request.Id} no existe.");
        }

        return conductor;
    }
}
