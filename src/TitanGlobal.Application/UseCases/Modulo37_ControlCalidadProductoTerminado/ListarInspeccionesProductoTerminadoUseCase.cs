using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class ListarInspeccionesProductoTerminadoUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarInspeccionesProductoTerminadoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaInspeccionesProductoTerminadoDto> ExecuteAsync(
        string? busqueda,
        int? ordenProduccionId,
        int? productoId,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        int pagina,
        int tamanoPagina)
    {
        ValidacionInspeccionProductoTerminado.ValidarPaginacion(pagina, tamanoPagina);
        string? texto = ValidacionInspeccionProductoTerminado.NormalizarBusqueda(busqueda);
        if (ordenProduccionId.HasValue)
        {
            ValidacionInspeccionProductoTerminado.ValidarIdOrden(ordenProduccionId.Value);
        }

        if (productoId.HasValue)
        {
            ValidacionInspeccionProductoTerminado.ValidarIdProducto(productoId.Value);
        }

        string? estadoNormalizado = estado == null
            ? null
            : ValidacionInspeccionProductoTerminado.NormalizarEstado(estado);
        ValidacionInspeccionProductoTerminado.ValidarRangoFechas(desde, hasta);

        IQueryable<InspeccionProductoTerminado> consulta =
            _unidad.Repository<InspeccionProductoTerminado>().AsQueryable();
        if (texto != null)
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas en el número propio o en el de la orden.
            consulta = consulta.Where(inspeccion => inspeccion.Numero.ToUpper().Contains(texto)
                || inspeccion.OrdenProduccion.Numero.ToUpper().Contains(texto));
        }

        if (ordenProduccionId.HasValue)
        {
            consulta = consulta.Where(inspeccion => inspeccion.OrdenProduccionId == ordenProduccionId.Value);
        }

        if (productoId.HasValue)
        {
            consulta = consulta.Where(inspeccion => inspeccion.OrdenProduccion.ProductoId == productoId.Value);
        }

        if (estadoNormalizado != null)
        {
            consulta = consulta.Where(inspeccion => inspeccion.Estado == estadoNormalizado);
        }

        // PATRÓN LINQ 6: filtro por rango de fecha.
        if (desde.HasValue)
        {
            consulta = consulta.Where(inspeccion => inspeccion.FechaInspeccion >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(inspeccion => inspeccion.FechaInspeccion <= hasta.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        if (inicio >= total)
        {
            return new PaginaInspeccionesProductoTerminadoDto(
                new List<InspeccionProductoTerminadoListadoDto>(), total, pagina, tamanoPagina);
        }

        // PATRÓN LINQ 3: proyección a DTO; el porcentaje se calcula después para redondear igual en todas las bases.
        List<FilaInspeccion> filas = await consulta
            .OrderByDescending(inspeccion => inspeccion.FechaInspeccion)
            .ThenByDescending(inspeccion => inspeccion.Id)
            .Skip((int)inicio)
            .Take(tamanoPagina)
            .Select(inspeccion => new FilaInspeccion(
                new InspeccionProductoTerminadoListadoDto(
                    inspeccion.Id,
                    inspeccion.Numero,
                    inspeccion.OrdenProduccionId,
                    inspeccion.OrdenProduccion.Numero,
                    inspeccion.OrdenProduccion.ProductoId,
                    inspeccion.OrdenProduccion.Producto.Nombre,
                    inspeccion.FechaInspeccion,
                    inspeccion.CantidadInspeccionada,
                    inspeccion.Estado,
                    null),
                inspeccion.CantidadNoConforme))
            .ToListAsync();
        List<InspeccionProductoTerminadoListadoDto> items = filas
            .Select(fila => fila.Listado with
            {
                PorcentajeNoConforme = ValidacionInspeccionProductoTerminado.PorcentajeNoConforme(
                    fila.CantidadNoConforme, fila.Listado.CantidadInspeccionada)
            })
            .ToList();
        return new PaginaInspeccionesProductoTerminadoDto(items, total, pagina, tamanoPagina);
    }

    private sealed record FilaInspeccion(InspeccionProductoTerminadoListadoDto Listado, int? CantidadNoConforme);
}
