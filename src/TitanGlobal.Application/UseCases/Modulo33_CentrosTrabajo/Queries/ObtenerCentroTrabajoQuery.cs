using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Queries;

public class ObtenerCentroTrabajoQuery : IRequest<CentroTrabajoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerCentroTrabajoQueryHandler : IRequestHandler<ObtenerCentroTrabajoQuery, CentroTrabajoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCentroTrabajoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CentroTrabajoDetalleDto> Handle(ObtenerCentroTrabajoQuery request, CancellationToken cancellationToken)
    {
        CentroTrabajoDetalleDto? centro = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .Where(entidad => entidad.Id == request.Id)
            .Select(entidad => new CentroTrabajoDetalleDto
            {
                Id = entidad.Id,
                Codigo = entidad.Codigo,
                Nombre = entidad.Nombre,
                Tipo = entidad.Tipo,
                Planta = entidad.Planta,
                CapacidadHorasDia = entidad.CapacidadHorasDia,
                CostoHora = entidad.CostoHora,
                Responsable = entidad.Responsable,
                Activo = entidad.Activo
            })
            .SingleOrDefaultAsync();
        if (centro is null)
        {
            throw PersistenciaCentroTrabajo.CrearNoEncontrado(request.Id);
        }

        return centro;
    }
}
