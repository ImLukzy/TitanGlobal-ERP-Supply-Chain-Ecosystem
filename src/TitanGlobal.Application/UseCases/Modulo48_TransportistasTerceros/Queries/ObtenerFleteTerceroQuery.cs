using MediatR;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Queries;

public class ObtenerFleteTerceroQuery : IRequest<FleteTerceroDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerFleteTerceroQueryHandler : IRequestHandler<ObtenerFleteTerceroQuery, FleteTerceroDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerFleteTerceroQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FleteTerceroDetalleDto> Handle(ObtenerFleteTerceroQuery request, CancellationToken cancellationToken)
    {
        FleteTerceroDetalleDto? detalle = await _unitOfWork.Repository<FleteTercero>().AsQueryable().Where(f => f.Id == request.Id)
            .Select(f => new FleteTerceroDetalleDto
            {
                Id = f.Id,
                TransportistaId = f.TransportistaTerceroId,
                TransportistaRazonSocial = f.TransportistaTercero.RazonSocial,
                Referencia = f.Referencia,
                Fecha = f.Fecha,
                Origen = f.Origen,
                Destino = f.Destino,
                Importe = f.Importe.ToString("0.00", CultureInfo.InvariantCulture),
                Moneda = f.Moneda,
                Estado = f.Estado,
                Observaciones = f.Observaciones,
                MotivoCancelacion = f.MotivoCancelacion,
                FechaCancelacion = f.FechaCancelacion
            }).FirstOrDefaultAsync();
        if (detalle is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El flete {request.Id} no existe."); }
        return detalle;
    }
}
