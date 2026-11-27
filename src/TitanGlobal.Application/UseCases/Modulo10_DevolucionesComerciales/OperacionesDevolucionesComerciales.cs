using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales;

// Lógica compartida por los commands y queries de DevolucionesComerciales.
internal static class OperacionesDevolucionesComerciales
{
    internal static async Task<DevolucionComercial> ObtenerEntidadAsync(IUnitOfWork unidadTrabajo, int id)
    {
        DevolucionComercial? devolucion = await unidadTrabajo.Repository<DevolucionComercial>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (devolucion is null)
        {
            throw CrearNoEncontrado(id);
        }

        return devolucion;
    }

    internal static async Task<string> ObtenerNumeroOrdenAsync(IUnitOfWork unidadTrabajo, int ordenId)
    {
        return await unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => orden.Id == ordenId)
            .Select(orden => orden.NumeroOrden)
            .SingleAsync();
    }

    internal static async Task<DevolucionDetalleDto> ProyectarDetalleAsync(IUnitOfWork unidadTrabajo, DevolucionComercial devolucion)
    {
        DevolucionListadoDto listado = (await ProyectarListadoAsync(unidadTrabajo, new List<DevolucionComercial> { devolucion }))[0];
        List<DetalleDevolucion> detalles = await unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .Where(detalle => detalle.DevolucionComercialId == devolucion.Id)
            .ToListAsync();
        List<int> productosIds = detalles.Select(detalle => detalle.ProductoId).ToList();
        List<Producto> productos = await unidadTrabajo.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id))
            .Select(producto => new Producto { Id = producto.Id, Nombre = producto.Nombre })
            .ToListAsync();
        List<DetalleOrdenCompra> detallesOrden = await unidadTrabajo.Repository<DetalleOrdenCompra>().AsQueryable()
            .Where(detalle => detalle.OrdenCompraId == devolucion.OrdenCompraId)
            .Select(detalle => new DetalleOrdenCompra
            {
                OrdenCompraId = detalle.OrdenCompraId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            })
            .ToListAsync();
        Dictionary<int, string> nombres = productos.ToDictionary(producto => producto.Id, producto => producto.Nombre);
        Dictionary<(int OrdenCompraId, int ProductoId), decimal> precios = CalcularPreciosUnitarios(detallesOrden);
        List<LineaDevolucionDto> lineas = detalles
            .OrderBy(detalle => detalle.ProductoId)
            .Select(detalle => CrearLineaDto(detalle, nombres, precios, devolucion.OrdenCompraId))
            .ToList();
        return new DevolucionDetalleDto
        {
            Id = listado.Id,
            Numero = listado.Numero,
            Fecha = listado.Fecha,
            ClienteId = listado.ClienteId,
            ClienteNombre = listado.ClienteNombre,
            OrdenCompraId = listado.OrdenCompraId,
            NumeroOrden = listado.NumeroOrden,
            ReclamoNumero = listado.ReclamoNumero,
            Motivo = listado.Motivo,
            Estado = listado.Estado,
            Unidades = listado.Unidades,
            Total = listado.Total,
            Observaciones = devolucion.Observaciones,
            MotivoRechazo = devolucion.MotivoRechazo,
            FechaAprobacion = devolucion.FechaAprobacion,
            FechaRecepcion = devolucion.FechaRecepcion,
            FechaRechazo = devolucion.FechaRechazo,
            Lineas = lineas
        };
    }

    internal static async Task<List<DevolucionListadoDto>> ProyectarListadoAsync(IUnitOfWork unidadTrabajo, List<DevolucionComercial> devoluciones)
    {
        List<int> devolucionIds = devoluciones.Select(devolucion => devolucion.Id).ToList();
        List<int> ordenesIds = devoluciones.Select(devolucion => devolucion.OrdenCompraId).Distinct().ToList();
        List<int> clientesIds = devoluciones.Select(devolucion => devolucion.ClienteId).Distinct().ToList();
        List<int> reclamosIds = devoluciones.Where(devolucion => devolucion.ReclamoPostventaId.HasValue)
            .Select(devolucion => devolucion.ReclamoPostventaId!.Value)
            .Distinct()
            .ToList();
        List<DetalleDevolucion> detalles = await unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .Where(detalle => devolucionIds.Contains(detalle.DevolucionComercialId))
            .ToListAsync();
        List<DetalleOrdenCompra> detallesOrdenes = await unidadTrabajo.Repository<DetalleOrdenCompra>().AsQueryable()
            .Where(detalle => ordenesIds.Contains(detalle.OrdenCompraId))
            .Select(detalle => new DetalleOrdenCompra
            {
                OrdenCompraId = detalle.OrdenCompraId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            })
            .ToListAsync();
        List<Cliente> clientes = await unidadTrabajo.Repository<Cliente>().AsQueryable()
            .Where(cliente => clientesIds.Contains(cliente.Id))
            .Select(cliente => new Cliente { Id = cliente.Id, Nombre = cliente.Nombre })
            .ToListAsync();
        List<OrdenCompra> ordenes = await unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => ordenesIds.Contains(orden.Id))
            .Select(orden => new OrdenCompra { Id = orden.Id, NumeroOrden = orden.NumeroOrden })
            .ToListAsync();
        List<ReclamoPostventa> reclamos = await unidadTrabajo.Repository<ReclamoPostventa>().AsQueryable()
            .Where(reclamo => reclamosIds.Contains(reclamo.Id))
            .Select(reclamo => new ReclamoPostventa { Id = reclamo.Id, Numero = reclamo.Numero })
            .ToListAsync();
        Dictionary<int, string> nombresClientes = clientes.ToDictionary(cliente => cliente.Id, cliente => cliente.Nombre);
        Dictionary<int, string> numerosOrdenes = ordenes.ToDictionary(orden => orden.Id, orden => orden.NumeroOrden);
        Dictionary<int, string> numerosReclamos = reclamos.ToDictionary(reclamo => reclamo.Id, reclamo => reclamo.Numero);
        Dictionary<(int OrdenCompraId, int ProductoId), decimal> precios = CalcularPreciosUnitarios(detallesOrdenes);
        Dictionary<int, List<DetalleDevolucion>> lineasPorDevolucion = detalles
            .GroupBy(detalle => detalle.DevolucionComercialId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());
        List<DevolucionListadoDto> resultado = new List<DevolucionListadoDto>();
        foreach (DevolucionComercial devolucion in devoluciones)
        {
            List<DetalleDevolucion> lineas = lineasPorDevolucion.GetValueOrDefault(devolucion.Id)
                ?? new List<DetalleDevolucion>();
            decimal total = lineas.Sum(linea => linea.Cantidad
                * precios.GetValueOrDefault((devolucion.OrdenCompraId, linea.ProductoId)));
            resultado.Add(new DevolucionListadoDto
            {
                Id = devolucion.Id,
                Numero = devolucion.Numero,
                Fecha = devolucion.Fecha,
                ClienteId = devolucion.ClienteId,
                ClienteNombre = nombresClientes.GetValueOrDefault(devolucion.ClienteId, string.Empty),
                OrdenCompraId = devolucion.OrdenCompraId,
                NumeroOrden = numerosOrdenes.GetValueOrDefault(devolucion.OrdenCompraId, string.Empty),
                ReclamoNumero = devolucion.ReclamoPostventaId.HasValue
                    ? numerosReclamos.GetValueOrDefault(devolucion.ReclamoPostventaId.Value)
                    : null,
                Motivo = devolucion.Motivo,
                Estado = devolucion.Estado,
                Unidades = lineas.Sum(linea => linea.Cantidad),
                Total = total
            });
        }

        return resultado;
    }

    internal static async Task<List<ProductoDisponibleDto>> CalcularDisponiblesAsync(IUnitOfWork unidadTrabajo, int ordenId, int? excluirDevolucionId)
    {
        List<DetalleOrdenCompra> detallesOrden = await unidadTrabajo.Repository<DetalleOrdenCompra>().AsQueryable()
            .Where(detalle => detalle.OrdenCompraId == ordenId)
            .Select(detalle => new DetalleOrdenCompra
            {
                OrdenCompraId = detalle.OrdenCompraId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            })
            .ToListAsync();
        List<int> devolucionesIds = await unidadTrabajo.Repository<DevolucionComercial>().AsQueryable()
            .Where(devolucion => devolucion.OrdenCompraId == ordenId
                && devolucion.Estado != "Rechazada"
                && (!excluirDevolucionId.HasValue || devolucion.Id != excluirDevolucionId.Value))
            .Select(devolucion => devolucion.Id)
            .ToListAsync();
        List<DetalleDevolucion> lineasComprometidas = await unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .Where(detalle => devolucionesIds.Contains(detalle.DevolucionComercialId))
            .Select(detalle => new DetalleDevolucion
            {
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad
            })
            .ToListAsync();
        Dictionary<int, int> cantidadesDevueltas = lineasComprometidas
            .GroupBy(detalle => detalle.ProductoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(detalle => detalle.Cantidad));
        List<int> productosIds = detallesOrden.Select(detalle => detalle.ProductoId).Distinct().ToList();
        List<Producto> productos = await unidadTrabajo.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id))
            .Select(producto => new Producto { Id = producto.Id, Nombre = producto.Nombre })
            .ToListAsync();
        Dictionary<int, string> nombresProductos = productos.ToDictionary(producto => producto.Id, producto => producto.Nombre);
        List<ProductoDisponibleDto> resultado = detallesOrden
            .GroupBy(detalle => detalle.ProductoId)
            .Select(grupo =>
            {
                int comprado = grupo.Sum(detalle => detalle.Cantidad);
                decimal montoOrden = grupo.Sum(detalle => detalle.Cantidad * detalle.PrecioUnitario);
                decimal precio = decimal.Round(montoOrden / comprado, 2, MidpointRounding.AwayFromZero);
                int devuelto = cantidadesDevueltas.GetValueOrDefault(grupo.Key);
                return new ProductoDisponibleDto
                {
                    ProductoId = grupo.Key,
                    ProductoNombre = nombresProductos.GetValueOrDefault(grupo.Key, string.Empty),
                    Comprado = comprado,
                    Devuelto = devuelto,
                    Disponible = comprado - devuelto,
                    PrecioUnitario = precio
                };
            })
            .OrderBy(producto => producto.ProductoNombre)
            .ToList();
        return resultado;
    }

    internal static Dictionary<(int OrdenCompraId, int ProductoId), decimal> CalcularPreciosUnitarios(
        List<DetalleOrdenCompra> detallesOrdenes)
    {
        return detallesOrdenes
            .GroupBy(detalle => new { detalle.OrdenCompraId, detalle.ProductoId })
            .ToDictionary(grupo => (grupo.Key.OrdenCompraId, grupo.Key.ProductoId), grupo => CalcularPrecioUnitario(grupo));
    }

    internal static decimal CalcularPrecioUnitario(IEnumerable<DetalleOrdenCompra> detalles)
    {
        decimal unidades = detalles.Sum(detalle => detalle.Cantidad);
        decimal total = detalles.Sum(detalle => detalle.Cantidad * detalle.PrecioUnitario);
        return decimal.Round(total / unidades, 2, MidpointRounding.AwayFromZero);
    }

    internal static LineaDevolucionDto CrearLineaDto(
        DetalleDevolucion detalle,
        Dictionary<int, string> nombres,
        Dictionary<(int OrdenCompraId, int ProductoId), decimal> precios,
        int ordenId)
    {
        decimal precio = precios.GetValueOrDefault((ordenId, detalle.ProductoId));
        return new LineaDevolucionDto
        {
            ProductoId = detalle.ProductoId,
            ProductoNombre = nombres.GetValueOrDefault(detalle.ProductoId, string.Empty),
            Cantidad = detalle.Cantidad,
            PrecioUnitario = precio,
            Importe = detalle.Cantidad * precio
        };
    }

    internal static void ValidarCantidadesDisponibles(
        GuardarDevolucionRequestDto solicitud,
        List<ProductoDisponibleDto> disponibles,
        string numeroOrden)
    {
        Dictionary<int, ProductoDisponibleDto> porProducto = disponibles.ToDictionary(producto => producto.ProductoId, producto => producto);
        foreach (LineaGuardarDevolucionDto linea in solicitud.Lineas)
        {
            ProductoDisponibleDto disponible = porProducto[linea.ProductoId];
            if (linea.Cantidad > disponible.Disponible)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict,
                    $"Solo quedan {disponible.Disponible} unidades de {disponible.ProductoNombre} por devolver en la orden {numeroOrden}.");
            }
        }
    }

    internal static void AgregarLineas(DevolucionComercial devolucion, List<LineaGuardarDevolucionDto> lineas)
    {
        devolucion.Detalles = lineas.Select(linea => new DetalleDevolucion
        {
            ProductoId = linea.ProductoId,
            Cantidad = linea.Cantidad
        }).ToList();
    }

    internal static TransactionScope CrearTransaccion()
    {
        TransactionOptions opciones = new TransactionOptions
        {
            IsolationLevel = System.Transactions.IsolationLevel.Serializable
        };
        return new TransactionScope(
            TransactionScopeOption.Required,
            opciones,
            TransactionScopeAsyncFlowOption.Enabled);
    }

    internal static DevolucionCreadaDto CrearDtoCreado(DevolucionComercial devolucion)
    {
        return new DevolucionCreadaDto { Id = devolucion.Id, Numero = devolucion.Numero };
    }

    internal static void ExigirEstado(DevolucionComercial devolucion, params string[] estadosPermitidos)
    {
        if (!estadosPermitidos.Contains(devolucion.Estado))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La devolución {devolucion.Numero} está {ValidacionDevolucion.EstadoLegible(devolucion.Estado)}.");
        }
    }

    internal static ReglaNegocioException CrearNoEncontrado(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La devolución {id} no existe.");
    }
}
