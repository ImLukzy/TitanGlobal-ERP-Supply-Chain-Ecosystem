using MediatR;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Queries;

public class ObtenerConsolidacionQuery : IRequest<ConsolidacionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerConsolidacionQueryHandler : IRequestHandler<ObtenerConsolidacionQuery, ConsolidacionDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerConsolidacionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ConsolidacionDetalleDto> Handle(ObtenerConsolidacionQuery request, CancellationToken cancellationToken)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, request.Id);
        return CalculoCarga.Detalle(carga);
    }
}
