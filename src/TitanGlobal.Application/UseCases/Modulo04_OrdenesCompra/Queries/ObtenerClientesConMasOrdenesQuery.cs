using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerClientesConMasOrdenesQuery : IRequest<List<ClienteFrecuenciaDto>>
{
}

public class ObtenerClientesConMasOrdenesQueryHandler : IRequestHandler<ObtenerClientesConMasOrdenesQuery, List<ClienteFrecuenciaDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerClientesConMasOrdenesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ClienteFrecuenciaDto>> Handle(ObtenerClientesConMasOrdenesQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 9: agrupación y orden por conteo.
        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .GroupBy(orden => orden.ClienteId)
            .Select(group => new ClienteFrecuenciaDto
            {
                ClienteId = group.Key,
                TotalOrdenes = group.Count()
            })
            .OrderByDescending(cliente => cliente.TotalOrdenes)
            .ThenBy(cliente => cliente.ClienteId)
            .ToListAsync();
    }
}
