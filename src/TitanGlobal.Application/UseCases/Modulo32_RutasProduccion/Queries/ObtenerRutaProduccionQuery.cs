using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion.Queries;

public class ObtenerRutaProduccionQuery : IRequest<RutaProduccionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerRutaProduccionQueryHandler : IRequestHandler<ObtenerRutaProduccionQuery, RutaProduccionDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerRutaProduccionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<RutaProduccionDetalleDto> Handle(ObtenerRutaProduccionQuery request, CancellationToken cancellationToken)
    {
        RutaProduccionDetalleDto? detalle = await _unidad.Repository<RutaProduccion>().AsQueryable()
            .Where(ruta => ruta.Id == request.Id)
            .Select(ruta => new RutaProduccionDetalleDto(
                ruta.Id, ruta.ProductoId, ruta.Producto.Nombre, ruta.Version, ruta.Estado,
                ruta.OperacionesRutaProduccion.OrderBy(operacion => operacion.Secuencia)
                    .Select(operacion => new OperacionRutaProduccionDto(
                        operacion.Secuencia, operacion.Nombre, operacion.CentroTrabajoId,
                        operacion.CentroTrabajo.Nombre, operacion.MinutosPreparacion, operacion.MinutosPorUnidad))
                    .ToList()))
            .SingleOrDefaultAsync();
        return detalle ?? throw ValidacionRutaProduccion.NoEncontrada(request.Id);
    }
}
