using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo10_DevolucionesComerciales.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo10_DevolucionesComerciales.UseCases;

public class DevolucionesComercialesUseCase
{
    private static readonly string[] OrdenMotivos = new[]
    {
        "Defecto",
        "ErrorPedido",
        "Arrepentimiento",
        "Otro"
    };

    private readonly IUnitOfWork _unidadTrabajo;

    public DevolucionesComercialesUseCase(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<PaginaDevolucionesDto> ListarAsync(
        string? busqueda,
        string? estado,
        string? motivo,
        int? clienteId,
        int pagina,
        int tamanoPagina)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        string? estadoNormalizado = ValidacionDevolucion.NormalizarEstadoFiltro(estado);
        string? motivoNormalizado = ValidacionDevolucion.NormalizarMotivoFiltro(motivo);
        IQueryable<DevolucionComercial> consulta =
            _unidadTrabajo.Repository<DevolucionComercial>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(devolucion => devolucion.Numero.ToLower().Contains(termino)
                || _unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
                    .Any(orden => orden.Id == devolucion.OrdenCompraId
                        && orden.NumeroOrden.ToLower().Contains(termino))
                || _unidadTrabajo.Repository<Cliente>().AsQueryable()
                    .Any(cliente => cliente.Id == devolucion.ClienteId
                        && cliente.Nombre.ToLower().Contains(termino)));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(devolucion => devolucion.Estado == estadoNormalizado);
        }

        if (motivoNormalizado is not null)
        {
            consulta = consulta.Where(devolucion => devolucion.Motivo == motivoNormalizado);
        }

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(devolucion => devolucion.ClienteId == clienteId.Value);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<DevolucionComercial> devoluciones = await consulta
            .OrderByDescending(devolucion => devolucion.Fecha)
            .ThenByDescending(devolucion => devolucion.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .ToListAsync();
        List<DevolucionListadoDto> items = await ProyectarListadoAsync(devoluciones);
        return new PaginaDevolucionesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<DevolucionDetalleDto> ObtenerAsync(int id)
    {
        DevolucionComercial? devolucion = await _unidadTrabajo.Repository<DevolucionComercial>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (devolucion is null)
        {
            throw CrearNoEncontrado(id);
        }

        return await ProyectarDetalleAsync(devolucion);
    }

    public async Task<List<ProductoDisponibleDto>> ObtenerDisponiblesAsync(int ordenId)
    {
        bool ordenExiste = await _unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
            .AnyAsync(orden => orden.Id == ordenId);
        if (!ordenExiste)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                $"La orden de compra {ordenId} no existe.");
        }

        return await CalcularDisponiblesAsync(ordenId, null);
    }

    public async Task<DevolucionCreadaDto> CrearAsync(GuardarDevolucionRequestDto solicitud)
    {
        using (TransactionScope transaccion = CrearTransaccion())
        {
            GuardarDevolucionRequestDto datos = await ValidacionDevolucion.ValidarAsync(
                solicitud,
                _unidadTrabajo);
            List<ProductoDisponibleDto> disponibles = await CalcularDisponiblesAsync(datos.OrdenCompraId, null);
            string numeroOrden = await ObtenerNumeroOrdenAsync(datos.OrdenCompraId);
            ValidarCantidadesDisponibles(datos, disponibles, numeroOrden);
            DevolucionComercial devolucion = new DevolucionComercial
            {
                Numero = CrearNumeroTemporal(),
                Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
                ClienteId = datos.ClienteId,
                OrdenCompraId = datos.OrdenCompraId,
                ReclamoPostventaId = datos.ReclamoPostventaId,
                Motivo = datos.Motivo,
                Estado = "Solicitada",
                Observaciones = datos.Observaciones
            };
            try
            {
                await _unidadTrabajo.Repository<DevolucionComercial>().AddAsync(devolucion);
                await _unidadTrabajo.CompleteAsync();
                devolucion.Numero = CrearNumero(devolucion.Id);
                AgregarLineas(devolucion, datos.Lineas);
                _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
                await _unidadTrabajo.CompleteAsync();
                transaccion.Complete();
            }
            catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status409Conflict,
                    $"Ya existe la devolución {CrearNumero(devolucion.Id)}.");
            }

            return CrearDtoCreado(devolucion);
        }
    }

    public async Task<DevolucionCreadaDto> ActualizarAsync(int id, GuardarDevolucionRequestDto solicitud)
    {
        using (TransactionScope transaccion = CrearTransaccion())
        {
            GuardarDevolucionRequestDto datos = await ValidacionDevolucion.ValidarAsync(
                solicitud,
                _unidadTrabajo);
            DevolucionComercial devolucion = await ObtenerEntidadAsync(id);
            ExigirEstado(devolucion, "Solicitada");
            List<ProductoDisponibleDto> disponibles = await CalcularDisponiblesAsync(datos.OrdenCompraId, id);
            string numeroOrden = await ObtenerNumeroOrdenAsync(datos.OrdenCompraId);
            ValidarCantidadesDisponibles(datos, disponibles, numeroOrden);
            List<DetalleDevolucion> lineasExistentes = await _unidadTrabajo.Repository<DetalleDevolucion>()
                .AsQueryable()
                .Where(detalle => detalle.DevolucionComercialId == id)
                .ToListAsync();
            foreach (DetalleDevolucion lineaExistente in lineasExistentes)
            {
                _unidadTrabajo.Repository<DetalleDevolucion>().Remove(lineaExistente);
            }

            devolucion.ClienteId = datos.ClienteId;
            devolucion.OrdenCompraId = datos.OrdenCompraId;
            devolucion.ReclamoPostventaId = datos.ReclamoPostventaId;
            devolucion.Motivo = datos.Motivo;
            devolucion.Observaciones = datos.Observaciones;
            AgregarLineas(devolucion, datos.Lineas);
            _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
            await _unidadTrabajo.CompleteAsync();
            transaccion.Complete();
            return CrearDtoCreado(devolucion);
        }
    }

    public async Task<DevolucionDetalleDto> AprobarAsync(int id)
    {
        DevolucionComercial devolucion = await ObtenerEntidadAsync(id);
        ExigirEstado(devolucion, "Solicitada");
        devolucion.Estado = "Aprobada";
        devolucion.FechaAprobacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
        await _unidadTrabajo.CompleteAsync();
        return await ProyectarDetalleAsync(devolucion);
    }

    public async Task<DevolucionDetalleDto> RecibirAsync(int id)
    {
        DevolucionComercial devolucion = await ObtenerEntidadAsync(id);
        ExigirEstado(devolucion, "Aprobada");
        devolucion.Estado = "Recibida";
        devolucion.FechaRecepcion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
        await _unidadTrabajo.CompleteAsync();
        return await ProyectarDetalleAsync(devolucion);
    }

    public async Task<DevolucionDetalleDto> RechazarAsync(int id, RechazarDevolucionRequestDto solicitud)
    {
        DevolucionComercial devolucion = await ObtenerEntidadAsync(id);
        ExigirEstado(devolucion, "Solicitada", "Aprobada");
        devolucion.MotivoRechazo = ValidacionDevolucion.NormalizarMotivoRechazo(solicitud.Motivo);
        devolucion.Estado = "Rechazada";
        devolucion.FechaRechazo = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
        await _unidadTrabajo.CompleteAsync();
        return await ProyectarDetalleAsync(devolucion);
    }

    public async Task EliminarAsync(int id)
    {
        DevolucionComercial devolucion = await ObtenerEntidadAsync(id);
        ExigirEstado(devolucion, "Solicitada");
        List<DetalleDevolucion> lineas = await _unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .Where(detalle => detalle.DevolucionComercialId == id)
            .ToListAsync();
        foreach (DetalleDevolucion linea in lineas)
        {
            _unidadTrabajo.Repository<DetalleDevolucion>().Remove(linea);
        }

        _unidadTrabajo.Repository<DevolucionComercial>().Remove(devolucion);
        await _unidadTrabajo.CompleteAsync();
    }

    public async Task<ResumenDevolucionesDto> ResumenAsync()
    {
        List<DevolucionComercial> devoluciones = await _unidadTrabajo.Repository<DevolucionComercial>()
            .AsQueryable()
            .ToListAsync();
        List<int> idsRecibidas = devoluciones
            .Where(devolucion => devolucion.Estado == "Recibida")
            .Select(devolucion => devolucion.Id)
            .ToList();
        List<DetalleDevolucion> detalles = await _unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .ToListAsync();
        List<DetalleOrdenCompra> detallesOrdenes = await _unidadTrabajo.Repository<DetalleOrdenCompra>()
            .AsQueryable()
            .Select(detalle => new DetalleOrdenCompra
            {
                OrdenCompraId = detalle.OrdenCompraId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            })
            .ToListAsync();
        Dictionary<(int OrdenCompraId, int ProductoId), decimal> precios = CalcularPreciosUnitarios(detallesOrdenes);
        Dictionary<int, int> ordenesDevoluciones = devoluciones.ToDictionary(devolucion => devolucion.Id, devolucion => devolucion.OrdenCompraId);
        decimal montoReembolsado = detalles
            .Where(detalle => idsRecibidas.Contains(detalle.DevolucionComercialId))
            .Sum(detalle => detalle.Cantidad
                * precios.GetValueOrDefault((ordenesDevoluciones[detalle.DevolucionComercialId], detalle.ProductoId)));
        int unidadesRecibidas = detalles
            .Where(detalle => idsRecibidas.Contains(detalle.DevolucionComercialId))
            .Sum(detalle => detalle.Cantidad);
        int unidadesVendidas = detallesOrdenes.Sum(detalle => detalle.Cantidad);
        decimal tasa = unidadesVendidas == 0
            ? 0m
            : decimal.Round(unidadesRecibidas * 100m / unidadesVendidas, 2, MidpointRounding.AwayFromZero);
        List<MotivoDevolucionesDto> porMotivo = new List<MotivoDevolucionesDto>();
        foreach (string motivo in OrdenMotivos)
        {
            List<DevolucionComercial> porMotivoActual = devoluciones
                .Where(devolucion => devolucion.Motivo == motivo)
                .ToList();
            if (porMotivoActual.Count == 0)
            {
                continue;
            }

            List<int> idsMotivo = porMotivoActual.Select(devolucion => devolucion.Id).ToList();
            porMotivo.Add(new MotivoDevolucionesDto
            {
                Motivo = motivo,
                Devoluciones = porMotivoActual.Count,
                Unidades = detalles.Where(detalle => idsMotivo.Contains(detalle.DevolucionComercialId))
                    .Sum(detalle => detalle.Cantidad)
            });
        }

        return new ResumenDevolucionesDto
        {
            Total = devoluciones.Count,
            Solicitadas = devoluciones.Count(devolucion => devolucion.Estado == "Solicitada"),
            Aprobadas = devoluciones.Count(devolucion => devolucion.Estado == "Aprobada"),
            Recibidas = devoluciones.Count(devolucion => devolucion.Estado == "Recibida"),
            Rechazadas = devoluciones.Count(devolucion => devolucion.Estado == "Rechazada"),
            MontoReembolsado = montoReembolsado,
            PorMotivo = porMotivo,
            TasaDevolucion = tasa
        };
    }

    private async Task<DevolucionComercial> ObtenerEntidadAsync(int id)
    {
        DevolucionComercial? devolucion = await _unidadTrabajo.Repository<DevolucionComercial>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (devolucion is null)
        {
            throw CrearNoEncontrado(id);
        }

        return devolucion;
    }

    private async Task<string> ObtenerNumeroOrdenAsync(int ordenId)
    {
        return await _unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => orden.Id == ordenId)
            .Select(orden => orden.NumeroOrden)
            .SingleAsync();
    }

    private async Task<DevolucionDetalleDto> ProyectarDetalleAsync(DevolucionComercial devolucion)
    {
        DevolucionListadoDto listado = (await ProyectarListadoAsync(new List<DevolucionComercial> { devolucion }))[0];
        List<DetalleDevolucion> detalles = await _unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .Where(detalle => detalle.DevolucionComercialId == devolucion.Id)
            .ToListAsync();
        List<int> productosIds = detalles.Select(detalle => detalle.ProductoId).ToList();
        List<Producto> productos = await _unidadTrabajo.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id))
            .Select(producto => new Producto { Id = producto.Id, Nombre = producto.Nombre })
            .ToListAsync();
        List<DetalleOrdenCompra> detallesOrden = await _unidadTrabajo.Repository<DetalleOrdenCompra>().AsQueryable()
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

    private async Task<List<DevolucionListadoDto>> ProyectarListadoAsync(List<DevolucionComercial> devoluciones)
    {
        List<int> devolucionIds = devoluciones.Select(devolucion => devolucion.Id).ToList();
        List<int> ordenesIds = devoluciones.Select(devolucion => devolucion.OrdenCompraId).Distinct().ToList();
        List<int> clientesIds = devoluciones.Select(devolucion => devolucion.ClienteId).Distinct().ToList();
        List<int> reclamosIds = devoluciones.Where(devolucion => devolucion.ReclamoPostventaId.HasValue)
            .Select(devolucion => devolucion.ReclamoPostventaId!.Value)
            .Distinct()
            .ToList();
        List<DetalleDevolucion> detalles = await _unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
            .Where(detalle => devolucionIds.Contains(detalle.DevolucionComercialId))
            .ToListAsync();
        List<DetalleOrdenCompra> detallesOrdenes = await _unidadTrabajo.Repository<DetalleOrdenCompra>().AsQueryable()
            .Where(detalle => ordenesIds.Contains(detalle.OrdenCompraId))
            .Select(detalle => new DetalleOrdenCompra
            {
                OrdenCompraId = detalle.OrdenCompraId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            })
            .ToListAsync();
        List<Cliente> clientes = await _unidadTrabajo.Repository<Cliente>().AsQueryable()
            .Where(cliente => clientesIds.Contains(cliente.Id))
            .Select(cliente => new Cliente { Id = cliente.Id, Nombre = cliente.Nombre })
            .ToListAsync();
        List<OrdenCompra> ordenes = await _unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => ordenesIds.Contains(orden.Id))
            .Select(orden => new OrdenCompra { Id = orden.Id, NumeroOrden = orden.NumeroOrden })
            .ToListAsync();
        List<ReclamoPostventa> reclamos = await _unidadTrabajo.Repository<ReclamoPostventa>().AsQueryable()
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

    private async Task<List<ProductoDisponibleDto>> CalcularDisponiblesAsync(int ordenId, int? excluirDevolucionId)
    {
        List<DetalleOrdenCompra> detallesOrden = await _unidadTrabajo.Repository<DetalleOrdenCompra>().AsQueryable()
            .Where(detalle => detalle.OrdenCompraId == ordenId)
            .Select(detalle => new DetalleOrdenCompra
            {
                OrdenCompraId = detalle.OrdenCompraId,
                ProductoId = detalle.ProductoId,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario
            })
            .ToListAsync();
        List<int> devolucionesIds = await _unidadTrabajo.Repository<DevolucionComercial>().AsQueryable()
            .Where(devolucion => devolucion.OrdenCompraId == ordenId
                && devolucion.Estado != "Rechazada"
                && (!excluirDevolucionId.HasValue || devolucion.Id != excluirDevolucionId.Value))
            .Select(devolucion => devolucion.Id)
            .ToListAsync();
        List<DetalleDevolucion> lineasComprometidas = await _unidadTrabajo.Repository<DetalleDevolucion>().AsQueryable()
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
        List<Producto> productos = await _unidadTrabajo.Repository<Producto>().AsQueryable()
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

    private static Dictionary<(int OrdenCompraId, int ProductoId), decimal> CalcularPreciosUnitarios(
        List<DetalleOrdenCompra> detallesOrdenes)
    {
        return detallesOrdenes
            .GroupBy(detalle => new { detalle.OrdenCompraId, detalle.ProductoId })
            .ToDictionary(grupo => (grupo.Key.OrdenCompraId, grupo.Key.ProductoId), grupo => CalcularPrecioUnitario(grupo));
    }

    private static decimal CalcularPrecioUnitario(IEnumerable<DetalleOrdenCompra> detalles)
    {
        decimal unidades = detalles.Sum(detalle => detalle.Cantidad);
        decimal total = detalles.Sum(detalle => detalle.Cantidad * detalle.PrecioUnitario);
        return decimal.Round(total / unidades, 2, MidpointRounding.AwayFromZero);
    }

    private static LineaDevolucionDto CrearLineaDto(
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

    private static void ValidarCantidadesDisponibles(
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
                    StatusCodes.Status409Conflict,
                    $"Solo quedan {disponible.Disponible} unidades de {disponible.ProductoNombre} por devolver en la orden {numeroOrden}.");
            }
        }
    }

    private static void AgregarLineas(DevolucionComercial devolucion, List<LineaGuardarDevolucionDto> lineas)
    {
        devolucion.Detalles = lineas.Select(linea => new DetalleDevolucion
        {
            ProductoId = linea.ProductoId,
            Cantidad = linea.Cantidad
        }).ToList();
    }

    private static TransactionScope CrearTransaccion()
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

    private static string CrearNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    private static string CrearNumero(int id)
    {
        return $"DEV-{id:D6}";
    }

    private static DevolucionCreadaDto CrearDtoCreado(DevolucionComercial devolucion)
    {
        return new DevolucionCreadaDto { Id = devolucion.Id, Numero = devolucion.Numero };
    }

    private static void ExigirEstado(DevolucionComercial devolucion, params string[] estadosPermitidos)
    {
        if (!estadosPermitidos.Contains(devolucion.Estado))
        {
            throw new ReglaNegocioException(
                StatusCodes.Status409Conflict,
                $"La devolución {devolucion.Numero} está {ValidacionDevolucion.EstadoLegible(devolucion.Estado)}.");
        }
    }

    private static ReglaNegocioException CrearNoEncontrado(int id)
    {
        return new ReglaNegocioException(StatusCodes.Status404NotFound, $"La devolución {id} no existe.");
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }
    }

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        return excepcion.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
