using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

public class ListarRutasProduccionUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarRutasProduccionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaRutasProduccionDto> ExecuteAsync(
        int? productoId, string? estado, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionRutaProduccion.Invalida("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionRutaProduccion.Invalida("El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<RutaProduccion> consulta = _unidad.Repository<RutaProduccion>().AsQueryable();
        if (productoId.HasValue)
        {
            ValidacionRutaProduccion.ValidarIdProducto(productoId.Value);
            consulta = consulta.Where(ruta => ruta.ProductoId == productoId.Value);
        }

        if (estado != null)
        {
            string estadoNormalizado = ValidacionRutaProduccion.NormalizarEstado(estado);
            consulta = consulta.Where(ruta => ruta.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        if (inicio >= total)
        {
            return new PaginaRutasProduccionDto(new List<RutaProduccionListadoDto>(), total, pagina, tamanoPagina);
        }

        List<RutaProduccionListadoDto> items = await consulta
            .OrderBy(ruta => ruta.ProductoId)
            .ThenByDescending(ruta => ruta.Version)
            .Skip((int)inicio)
            .Take(tamanoPagina)
            .Select(ruta => new RutaProduccionListadoDto(
                ruta.Id, ruta.ProductoId, ruta.Producto.Nombre, ruta.Version, ruta.Estado,
                ruta.OperacionesRutaProduccion.Count))
            .ToListAsync();
        return new PaginaRutasProduccionDto(items, total, pagina, tamanoPagina);
    }
}
