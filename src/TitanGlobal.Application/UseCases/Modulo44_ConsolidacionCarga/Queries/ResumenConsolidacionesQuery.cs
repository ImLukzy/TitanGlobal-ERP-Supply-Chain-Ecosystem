using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Queries;

public class ResumenConsolidacionesQuery : IRequest<ResumenConsolidacionesDto>
{
}

public class ResumenConsolidacionesQueryHandler : IRequestHandler<ResumenConsolidacionesQuery, ResumenConsolidacionesDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenConsolidacionesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenConsolidacionesDto> Handle(ResumenConsolidacionesQuery request, CancellationToken cancellationToken)
    {
        List<ResumenConsolidacionesDto> resumen = await _unidad.Repository<ConsolidacionCarga>().AsQueryable().GroupBy(c => 1)
            .Select(grupo => new ResumenConsolidacionesDto { Total = grupo.Count(), Borradores = grupo.Count(c => c.Estado == "Borrador"), Confirmadas = grupo.Count(c => c.Estado == "Confirmada"), Canceladas = grupo.Count(c => c.Estado == "Cancelada") }).ToListAsync();
        return resumen.Count == 0 ? new ResumenConsolidacionesDto() : resumen[0];
    }
}
