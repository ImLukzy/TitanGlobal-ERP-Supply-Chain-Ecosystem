using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class ListarOrdenesProduccionUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarOrdenesProduccionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaOrdenesProduccionDto> ExecuteAsync(
        string? busqueda,
        int? productoId,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        int pagina,
        int tamanoPagina)
    {
        ValidacionOrdenProduccion.ValidarPaginacion(pagina, tamanoPagina);
        if (productoId.HasValue)
        {
            ValidacionOrdenProduccion.ValidarIdProducto(productoId.Value);
        }

        string? estadoNormalizado = estado == null ? null : ValidacionOrdenProduccion.NormalizarEstado(estado);
        ValidacionOrdenProduccion.ValidarRangoFechas(desde, hasta);
        string? numero = ValidacionOrdenProduccion.NormalizarBusqueda(busqueda);

        IQueryable<OrdenProduccion> consulta = _unidad.Repository<OrdenProduccion>().AsQueryable();
        if (numero != null)
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(orden => orden.Numero.ToUpper().Contains(numero));
        }

        if (productoId.HasValue)
        {
            consulta = consulta.Where(orden => orden.ProductoId == productoId.Value);
        }

        if (estadoNormalizado != null)
        {
            consulta = consulta.Where(orden => orden.Estado == estadoNormalizado);
        }

        // PATRÓN LINQ 6: filtro por rango de fecha.
        if (desde.HasValue)
        {
            consulta = consulta.Where(orden => orden.FechaInicioPlanificada >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(orden => orden.FechaInicioPlanificada <= hasta.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        if (inicio >= total)
        {
            return new PaginaOrdenesProduccionDto(new List<OrdenProduccionListadoDto>(), total, pagina, tamanoPagina);
        }

        // PATRÓN LINQ 3: proyección directa a DTO.
        List<OrdenProduccionListadoDto> items = await consulta
            .OrderByDescending(orden => orden.FechaInicioPlanificada)
            .ThenByDescending(orden => orden.Id)
            .Skip((int)inicio)
            .Take(tamanoPagina)
            .Select(orden => new OrdenProduccionListadoDto(
                orden.Id,
                orden.Numero,
                orden.ProductoId,
                orden.Producto.Nombre,
                orden.CantidadPlanificada,
                orden.FechaInicioPlanificada,
                orden.FechaFinPlanificada,
                orden.Estado))
            .ToListAsync();
        return new PaginaOrdenesProduccionDto(items, total, pagina, tamanoPagina);
    }
}
