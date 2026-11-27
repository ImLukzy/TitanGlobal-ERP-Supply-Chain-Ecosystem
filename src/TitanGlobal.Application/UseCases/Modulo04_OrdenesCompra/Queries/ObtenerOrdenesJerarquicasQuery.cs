using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Queries;

public class ObtenerOrdenesJerarquicasQuery : IRequest<List<OrdenCompraJerarquicaDto>>
{
    public int ClienteId { get; set; }
}

public class ObtenerOrdenesJerarquicasQueryHandler : IRequestHandler<ObtenerOrdenesJerarquicasQuery, List<OrdenCompraJerarquicaDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerOrdenesJerarquicasQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<OrdenCompraJerarquicaDto>> Handle(ObtenerOrdenesJerarquicasQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 10: proyección jerárquica padre e hijos.
        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.ClienteId == request.ClienteId)
            .OrderBy(orden => orden.Id)
            .Select(orden => new OrdenCompraJerarquicaDto
            {
                OrdenId = orden.Id,
                NumeroOrden = orden.NumeroOrden,
                Fecha = orden.Fecha,
                NombreCliente = orden.Cliente.Nombre,
                Detalles = orden.Detalles
                    .OrderBy(detalle => detalle.Id)
                    .Select(detalle => new DetalleOrdenItemDto
                    {
                        ProductoId = detalle.ProductoId,
                        NombreProducto = detalle.Producto.Nombre,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario
                    })
                    .ToList()
            })
            .ToListAsync();
    }
}
