using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

public class ObtenerListaMaterialUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerListaMaterialUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ListaMaterialDetalleDto> ExecuteAsync(int id)
    {
        ListaMaterialDetalleDto? detalle = await _unidad.Repository<ListaMateriales>().AsQueryable()
            .Where(lista => lista.Id == id)
            .Select(lista => new ListaMaterialDetalleDto(
                lista.Id, lista.ProductoId, lista.Producto.Nombre, lista.Version, lista.Estado,
                lista.DetallesListaMaterial.OrderBy(linea => linea.ComponenteProductoId)
                    .Select(linea => new ComponenteListaMaterialDto(
                        linea.ComponenteProductoId, linea.ComponenteProducto.Nombre, linea.Cantidad, linea.UnidadMedida))
                    .ToList()))
            .SingleOrDefaultAsync();

        return detalle ?? throw ValidacionListaMaterial.NoEncontrada(id);
    }
}
