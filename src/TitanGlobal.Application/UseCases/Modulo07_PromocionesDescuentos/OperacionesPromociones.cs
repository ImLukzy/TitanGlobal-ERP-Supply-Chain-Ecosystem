using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos;

// Lógica compartida por los commands y queries de Promociones.
internal static class OperacionesPromociones
{
    internal static async Task<List<PromocionListadoDto>> ProyectarListadoAsync(IUnitOfWork unitOfWork, List<Promocion> promociones, DateTime hoy)
    {
        List<int> productosIds = promociones.Where(promocion => promocion.ProductoId.HasValue)
            .Select(promocion => promocion.ProductoId!.Value).Distinct().ToList();
        List<Producto> productos = await unitOfWork.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id)).ToListAsync();
        Dictionary<int, string> nombresProductos = productos.ToDictionary(producto => producto.Id, producto => producto.Nombre);
        List<int> segmentosIds = promociones.Where(promocion => promocion.SegmentoClienteId.HasValue)
            .Select(promocion => promocion.SegmentoClienteId!.Value).Distinct().ToList();
        List<SegmentoCliente> segmentos = await unitOfWork.Repository<SegmentoCliente>().AsQueryable()
            .Where(segmento => segmentosIds.Contains(segmento.Id)).ToListAsync();
        Dictionary<int, string> codigosSegmentos = segmentos.ToDictionary(segmento => segmento.Id, segmento => segmento.Codigo);
        List<PromocionListadoDto> resultado = new List<PromocionListadoDto>();
        foreach (Promocion promocion in promociones)
        {
            resultado.Add(new PromocionListadoDto
            {
                Id = promocion.Id,
                Codigo = promocion.Codigo,
                Nombre = promocion.Nombre,
                Tipo = promocion.Tipo,
                ProductoId = promocion.ProductoId,
                ProductoNombre = promocion.ProductoId.HasValue
                    ? nombresProductos.GetValueOrDefault(promocion.ProductoId.Value)
                    : null,
                MontoMinimo = promocion.MontoMinimo,
                PorcentajeDescuento = promocion.PorcentajeDescuento,
                SegmentoId = promocion.SegmentoClienteId,
                SegmentoCodigo = promocion.SegmentoClienteId.HasValue
                    ? codigosSegmentos.GetValueOrDefault(promocion.SegmentoClienteId.Value)
                    : null,
                FechaInicio = promocion.FechaInicio,
                FechaFin = promocion.FechaFin,
                Activa = promocion.Activa,
                Vigente = promocion.Activa && promocion.FechaInicio <= hoy && promocion.FechaFin >= hoy
            });
        }

        return resultado;
    }

    internal static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }
}
