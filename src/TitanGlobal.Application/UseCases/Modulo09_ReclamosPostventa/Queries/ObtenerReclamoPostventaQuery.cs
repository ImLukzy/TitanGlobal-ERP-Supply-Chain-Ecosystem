using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Queries;

public class ObtenerReclamoPostventaQuery : IRequest<ReclamoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerReclamoPostventaQueryHandler : IRequestHandler<ObtenerReclamoPostventaQuery, ReclamoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerReclamoPostventaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReclamoDetalleDto> Handle(ObtenerReclamoPostventaQuery request, CancellationToken cancellationToken)
    {
        ReclamoPostventa? reclamo = await _unitOfWork.Repository<ReclamoPostventa>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (reclamo is null)
        {
            throw OperacionesReclamosPostventa.CrearNoEncontrado(request.Id);
        }

        return await OperacionesReclamosPostventa.ProyectarDetalleAsync(_unitOfWork, reclamo, FechasUtc.Normalizar(DateTime.UtcNow));
    }
}
