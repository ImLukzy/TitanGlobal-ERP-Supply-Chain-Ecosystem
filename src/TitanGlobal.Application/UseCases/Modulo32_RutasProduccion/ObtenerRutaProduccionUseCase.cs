using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

public class ObtenerRutaProduccionUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerRutaProduccionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<RutaProduccionDetalleDto> ExecuteAsync(int id)
    {
        RutaProduccionDetalleDto? detalle = await _unidad.Repository<RutaProduccion>().AsQueryable()
            .Where(ruta => ruta.Id == id)
            .Select(ruta => new RutaProduccionDetalleDto(
                ruta.Id, ruta.ProductoId, ruta.Producto.Nombre, ruta.Version, ruta.Estado,
                ruta.OperacionesRutaProduccion.OrderBy(operacion => operacion.Secuencia)
                    .Select(operacion => new OperacionRutaProduccionDto(
                        operacion.Secuencia, operacion.Nombre, operacion.CentroTrabajoId,
                        operacion.CentroTrabajo.Nombre, operacion.MinutosPreparacion, operacion.MinutosPorUnidad))
                    .ToList()))
            .SingleOrDefaultAsync();
        return detalle ?? throw ValidacionRutaProduccion.NoEncontrada(id);
    }
}
