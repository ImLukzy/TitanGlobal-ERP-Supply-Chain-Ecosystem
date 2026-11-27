using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales.Queries;

public class ObtenerListaMaterialQuery : IRequest<ListaMaterialDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerListaMaterialQueryHandler : IRequestHandler<ObtenerListaMaterialQuery, ListaMaterialDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerListaMaterialQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ListaMaterialDetalleDto> Handle(ObtenerListaMaterialQuery request, CancellationToken cancellationToken)
    {
        ListaMaterialDetalleDto? detalle = await _unidad.Repository<ListaMateriales>().AsQueryable()
            .Where(lista => lista.Id == request.Id)
            .Select(lista => new ListaMaterialDetalleDto(
                lista.Id, lista.ProductoId, lista.Producto.Nombre, lista.Version, lista.Estado,
                lista.DetallesListaMaterial.OrderBy(linea => linea.ComponenteProductoId)
                    .Select(linea => new ComponenteListaMaterialDto(
                        linea.ComponenteProductoId, linea.ComponenteProducto.Nombre, linea.Cantidad, linea.UnidadMedida))
                    .ToList()))
            .SingleOrDefaultAsync();

        return detalle ?? throw ValidacionListaMaterial.NoEncontrada(request.Id);
    }
}
