using MediatR;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Queries;

public class ObtenerRutaDespachoQuery : IRequest<RutaDespachoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerRutaDespachoQueryHandler : IRequestHandler<ObtenerRutaDespachoQuery, RutaDespachoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerRutaDespachoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RutaDespachoDetalleDto> Handle(ObtenerRutaDespachoQuery request, CancellationToken cancellationToken)
    {
        RutaDespachoDetalleDto? detalle = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().Where(r => r.Id == request.Id)
            .Select(r => new RutaDespachoDetalleDto
            {
                Id = r.Id,
                Codigo = r.Codigo,
                Nombre = r.Nombre,
                Zona = r.Zona,
                Origen = r.Origen,
                Destino = r.Destino,
                Activo = r.Activo,
                DistanciaKm = r.DistanciaKm.ToString("0.00", CultureInfo.InvariantCulture),
                PeajeEstimado = r.PeajeEstimado.ToString("0.00", CultureInfo.InvariantCulture),
                Moneda = r.Moneda,
                Observaciones = r.Observaciones
            }).FirstOrDefaultAsync();
        if (detalle is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La ruta de despacho {request.Id} no existe."); }
        return detalle;
    }
}
