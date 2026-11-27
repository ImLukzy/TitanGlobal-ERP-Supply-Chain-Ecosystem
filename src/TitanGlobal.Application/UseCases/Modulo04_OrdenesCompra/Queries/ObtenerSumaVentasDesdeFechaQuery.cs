using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerSumaVentasDesdeFechaQuery : IRequest<decimal>
{
    public DateTime FechaInicio { get; set; }
}

public class ObtenerSumaVentasDesdeFechaQueryHandler : IRequestHandler<ObtenerSumaVentasDesdeFechaQuery, decimal>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerSumaVentasDesdeFechaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> Handle(ObtenerSumaVentasDesdeFechaQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 4: suma asíncrona.
        // PATRÓN LINQ 6: filtro por rango de fecha.
        DateTime fechaInicioUtc = FechasUtc.Normalizar(request.FechaInicio);

        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.Fecha > fechaInicioUtc)
            .SumAsync(orden => orden.Total);
    }
}
