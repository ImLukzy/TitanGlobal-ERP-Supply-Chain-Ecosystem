using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa;

// Lógica compartida por los commands y queries de ReclamosPostventa.
internal static class OperacionesReclamosPostventa
{
    internal static async Task<ReclamoPostventa> ObtenerEntidadAsync(IUnitOfWork unitOfWork, int id)
    {
        ReclamoPostventa? reclamo = await unitOfWork.Repository<ReclamoPostventa>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (reclamo is null)
        {
            throw CrearNoEncontrado(id);
        }

        return reclamo;
    }

    internal static async Task<ReclamoDetalleDto> ProyectarDetalleAsync(IUnitOfWork unitOfWork, ReclamoPostventa reclamo, DateTime ahora)
    {
        ReclamoListadoDto listado = (await ProyectarListadoAsync(unitOfWork, new List<ReclamoPostventa> { reclamo }, ahora))[0];
        return new ReclamoDetalleDto
        {
            Id = listado.Id,
            Numero = listado.Numero,
            Fecha = listado.Fecha,
            ClienteId = listado.ClienteId,
            ClienteNombre = listado.ClienteNombre,
            OrdenCompraId = listado.OrdenCompraId,
            NumeroOrden = listado.NumeroOrden,
            ProductoNombre = listado.ProductoNombre,
            Tipo = listado.Tipo,
            Prioridad = listado.Prioridad,
            Estado = listado.Estado,
            FechaLimite = listado.FechaLimite,
            Vencido = listado.Vencido,
            Descripcion = reclamo.Descripcion,
            Resolucion = reclamo.Resolucion,
            MotivoRechazo = reclamo.MotivoRechazo,
            FechaInicio = reclamo.FechaInicio,
            FechaResolucion = reclamo.FechaResolucion,
            FechaCierre = reclamo.FechaCierre,
            AtendidoATiempo = reclamo.FechaResolucion.HasValue
                ? reclamo.FechaResolucion.Value <= reclamo.FechaLimite
                : null
        };
    }

    internal static async Task<List<ReclamoListadoDto>> ProyectarListadoAsync(IUnitOfWork unitOfWork,
        List<ReclamoPostventa> reclamos,
        DateTime ahora)
    {
        List<int> clientesIds = reclamos.Select(reclamo => reclamo.ClienteId).Distinct().ToList();
        List<int> ordenesIds = reclamos.Select(reclamo => reclamo.OrdenCompraId).Distinct().ToList();
        List<int> productosIds = reclamos.Where(reclamo => reclamo.ProductoId.HasValue)
            .Select(reclamo => reclamo.ProductoId!.Value)
            .Distinct()
            .ToList();
        List<Cliente> clientes = await unitOfWork.Repository<Cliente>().AsQueryable()
            .Where(cliente => clientesIds.Contains(cliente.Id))
            .Select(cliente => new Cliente { Id = cliente.Id, Nombre = cliente.Nombre })
            .ToListAsync();
        List<OrdenCompra> ordenes = await unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => ordenesIds.Contains(orden.Id))
            .Select(orden => new OrdenCompra { Id = orden.Id, NumeroOrden = orden.NumeroOrden })
            .ToListAsync();
        List<Producto> productos = await unitOfWork.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id))
            .Select(producto => new Producto { Id = producto.Id, Nombre = producto.Nombre })
            .ToListAsync();
        Dictionary<int, string> nombresClientes = clientes.ToDictionary(cliente => cliente.Id, cliente => cliente.Nombre);
        Dictionary<int, string> numerosOrdenes = ordenes.ToDictionary(orden => orden.Id, orden => orden.NumeroOrden);
        Dictionary<int, string> nombresProductos = productos.ToDictionary(producto => producto.Id, producto => producto.Nombre);
        List<ReclamoListadoDto> resultado = new List<ReclamoListadoDto>();
        foreach (ReclamoPostventa reclamo in reclamos)
        {
            resultado.Add(new ReclamoListadoDto
            {
                Id = reclamo.Id,
                Numero = reclamo.Numero,
                Fecha = reclamo.Fecha,
                ClienteId = reclamo.ClienteId,
                ClienteNombre = nombresClientes.GetValueOrDefault(reclamo.ClienteId, string.Empty),
                OrdenCompraId = reclamo.OrdenCompraId,
                NumeroOrden = numerosOrdenes.GetValueOrDefault(reclamo.OrdenCompraId, string.Empty),
                ProductoNombre = reclamo.ProductoId.HasValue
                    ? nombresProductos.GetValueOrDefault(reclamo.ProductoId.Value)
                    : null,
                Tipo = reclamo.Tipo,
                Prioridad = reclamo.Prioridad,
                Estado = reclamo.Estado,
                FechaLimite = reclamo.FechaLimite,
                Vencido = EsVencido(reclamo, ahora)
            });
        }

        return resultado;
    }

    internal static bool EsVencido(ReclamoPostventa reclamo, DateTime ahora)
    {
        return (reclamo.Estado == "Abierto" || reclamo.Estado == "EnProceso")
            && reclamo.FechaLimite < ahora;
    }

    internal static DateTime CalcularFechaLimite(DateTime fecha, string prioridad)
    {
        int dias = prioridad == "Alta" ? 2 : prioridad == "Media" ? 5 : 10;
        return FechasUtc.Normalizar(fecha.AddDays(dias));
    }

    internal static void ExigirEstado(ReclamoPostventa reclamo, params string[] estadosPermitidos)
    {
        if (!estadosPermitidos.Contains(reclamo.Estado))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El reclamo {reclamo.Numero} está {ValidacionReclamo.EstadoLegible(reclamo.Estado)}.");
        }
    }

    internal static ReglaNegocioException CrearNoEncontrado(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El reclamo {id} no existe.");
    }
}
