using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.UseCases;

public class AnaliticaOrdenesCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AnaliticaOrdenesCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductoResumenDto>> BuscarProductosPorFiltroAsync(string termino, decimal minPrecio)
    {
        // PATRÓN LINQ 1: filtro de texto insensible a mayúsculas.
        // PATRÓN LINQ 2: filtro numérico por umbral.
        // PATRÓN LINQ 3: proyección directa a DTO.
        string terminoNormalizado = termino.ToLower();

        return await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => producto.Nombre.ToLower().Contains(terminoNormalizado))
            .Where(producto => producto.Precio > minPrecio)
            .OrderBy(producto => producto.Id)
            .Select(producto => new ProductoResumenDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Precio = producto.Precio
            })
            .ToListAsync();
    }

    public async Task<decimal> ObtenerSumaVentasDesdeFechaAsync(DateTime fechaInicio)
    {
        // PATRÓN LINQ 4: suma asíncrona.
        // PATRÓN LINQ 6: filtro por rango de fecha.
        DateTime fechaInicioUtc = FechasUtc.Normalizar(fechaInicio);

        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.Fecha > fechaInicioUtc)
            .SumAsync(orden => orden.Total);
    }

    public async Task<ProductoResumenDto?> ObtenerProductoMasCaroAsync()
    {
        // PATRÓN LINQ 5: orden descendente y primero.
        return await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .OrderByDescending(producto => producto.Precio)
            .ThenBy(producto => producto.Id)
            .Select(producto => new ProductoResumenDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Precio = producto.Precio
            })
            .FirstOrDefaultAsync();
    }

    public async Task<decimal> ObtenerPromedioTotalPorClienteAsync(int clienteId)
    {
        // PATRÓN LINQ 7: promedio con validación previa.
        IQueryable<OrdenCompra> orders = _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.ClienteId == clienteId);

        bool hasOrders = await orders.AnyAsync();
        if (!hasOrders)
        {
            return 0;
        }

        return await orders.AverageAsync(orden => orden.Total);
    }

    public async Task<List<ProductoResumenDto>> ObtenerProductosSinDescripcionAsync()
    {
        // PATRÓN LINQ 8: campos nulos o vacíos.
        return await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => string.IsNullOrEmpty(producto.Descripcion))
            .OrderBy(producto => producto.Id)
            .Select(producto => new ProductoResumenDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Precio = producto.Precio
            })
            .ToListAsync();
    }

    public async Task<List<ClienteFrecuenciaDto>> ObtenerClientesConMasOrdenesAsync()
    {
        // PATRÓN LINQ 9: agrupación y orden por conteo.
        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .GroupBy(orden => orden.ClienteId)
            .Select(group => new ClienteFrecuenciaDto
            {
                ClienteId = group.Key,
                TotalOrdenes = group.Count()
            })
            .OrderByDescending(cliente => cliente.TotalOrdenes)
            .ThenBy(cliente => cliente.ClienteId)
            .ToListAsync();
    }

    public async Task<List<OrdenCompraJerarquicaDto>> ObtenerOrdenesJerarquicasAsync(int clienteId)
    {
        // PATRÓN LINQ 10: proyección jerárquica padre e hijos.
        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.ClienteId == clienteId)
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

    public async Task<List<string>> ObtenerCategoriasUnicasCompradasPorClienteAsync(int clienteId)
    {
        // PATRÓN LINQ 11: aplanado y valores únicos.
        return await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .Where(orden => orden.ClienteId == clienteId)
            .SelectMany(orden => orden.Detalles)
            .Select(detalle => detalle.Producto.Categoria)
            .Distinct()
            .OrderBy(categoria => categoria)
            .ToListAsync();
    }

    public async Task<List<string>> ObtenerClientesQueCompraronProductoAsync(int productoId)
    {
        // PATRÓN LINQ 12: navegación inversa y valores únicos.
        return await _unitOfWork.Repository<DetalleOrdenCompra>()
            .AsQueryable()
            .Where(detalle => detalle.ProductoId == productoId)
            .Select(detalle => detalle.OrdenCompra.Cliente.Nombre)
            .Distinct()
            .OrderBy(nombre => nombre)
            .ToListAsync();
    }
}
