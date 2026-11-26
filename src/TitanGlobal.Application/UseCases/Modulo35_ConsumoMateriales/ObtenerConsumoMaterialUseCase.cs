using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

public class ObtenerConsumoMaterialUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerConsumoMaterialUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ConsumoMaterialDetalleDto> ExecuteAsync(int id)
    {
        ConsumoMaterialDetalleDto? detalle = await _unidad.Repository<ConsumoMaterial>().AsQueryable()
            .Where(consumo => consumo.Id == id)
            .Select(consumo => new ConsumoMaterialDetalleDto(
                consumo.Id,
                consumo.Numero,
                consumo.OrdenProduccionId,
                consumo.OrdenProduccion.Numero,
                consumo.ComponenteProductoId,
                consumo.ComponenteProducto.Nombre,
                consumo.Cantidad,
                consumo.UnidadMedida,
                consumo.FechaRegistro,
                consumo.Estado,
                consumo.OrdenProduccion.Producto.Nombre,
                consumo.Responsable,
                consumo.Observaciones,
                consumo.FechaAnulacion,
                consumo.MotivoAnulacion))
            .SingleOrDefaultAsync();
        return detalle ?? throw ValidacionConsumoMaterial.NoEncontrado(id);
    }
}
