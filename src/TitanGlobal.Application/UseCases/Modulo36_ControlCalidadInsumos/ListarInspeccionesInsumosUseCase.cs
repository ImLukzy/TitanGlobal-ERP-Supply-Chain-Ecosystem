using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

public class ListarInspeccionesInsumosUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarInspeccionesInsumosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaInspeccionesInsumosDto> ExecuteAsync(
        string? busqueda, int? productoId, int? proveedorId, string? estado, DateOnly? desde, DateOnly? hasta,
        int pagina, int tamanoPagina)
    {
        ValidacionInspeccionInsumo.ValidarPaginacion(pagina, tamanoPagina);
        string? termino = ValidacionInspeccionInsumo.NormalizarBusqueda(busqueda);
        IQueryable<InspeccionInsumo> consulta = _unidad.Repository<InspeccionInsumo>().AsQueryable();
        if (termino != null)
        {
            // PATRÓN LINQ 1: filtro de texto insensible a mayúsculas.
            consulta = consulta.Where(inspeccion => inspeccion.Numero.ToLower().Contains(termino)
                || inspeccion.Lote.ToLower().Contains(termino));
        }

        if (productoId.HasValue)
        {
            ValidacionInspeccionInsumo.ValidarIdProducto(productoId.Value);
            consulta = consulta.Where(inspeccion => inspeccion.ProductoId == productoId.Value);
        }

        if (proveedorId.HasValue)
        {
            ValidacionInspeccionInsumo.ValidarIdProveedor(proveedorId.Value);
            consulta = consulta.Where(inspeccion => inspeccion.ProveedorId == proveedorId.Value);
        }

        if (estado != null)
        {
            string estadoNormalizado = ValidacionInspeccionInsumo.NormalizarEstado(estado);
            consulta = consulta.Where(inspeccion => inspeccion.Estado == estadoNormalizado);
        }

        ValidacionInspeccionInsumo.ValidarRango(desde, hasta);
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
            return new PaginaInspeccionesInsumosDto(new List<InspeccionInsumoListadoDto>(), total, pagina, tamanoPagina);
        }

        // PATRÓN LINQ 5: orden descendente por fecha, paginado en la consulta.
        IQueryable<InspeccionInsumo> paginaConsulta = consulta
            .OrderByDescending(inspeccion => inspeccion.FechaInspeccion)
            .ThenByDescending(inspeccion => inspeccion.Id)
            .Skip((int)inicio)
            .Take(tamanoPagina);
        List<FilaInspeccionInsumo> filas = await ConsultaInspeccionInsumo.Proyectar(paginaConsulta).ToListAsync();
        List<InspeccionInsumoListadoDto> items = filas.Select(ConsultaInspeccionInsumo.ALista).ToList();
        return new PaginaInspeccionesInsumosDto(items, total, pagina, tamanoPagina);
    }
}
