using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

public class ListarConsumosMaterialesUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarConsumosMaterialesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaConsumosMaterialesDto> ExecuteAsync(
        string? busqueda,
        int? ordenProduccionId,
        int? componenteProductoId,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        int pagina,
        int tamanoPagina)
    {
        ValidacionConsumoMaterial.ValidarPaginacion(pagina, tamanoPagina);
        string? numero = ValidacionConsumoMaterial.NormalizarBusqueda(busqueda);
        if (ordenProduccionId.HasValue)
        {
            ValidacionConsumoMaterial.ValidarIdOrden(ordenProduccionId.Value);
        }

        if (componenteProductoId.HasValue)
        {
            ValidacionConsumoMaterial.ValidarIdComponente(componenteProductoId.Value);
        }

        string? estadoNormalizado = estado == null ? null : ValidacionConsumoMaterial.NormalizarEstado(estado);
        ValidacionConsumoMaterial.ValidarRangoFechas(desde, hasta);

        IQueryable<ConsumoMaterial> consulta = _unidad.Repository<ConsumoMaterial>().AsQueryable();
        if (numero != null)
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(consumo => consumo.Numero.ToUpper().Contains(numero));
        }

        if (ordenProduccionId.HasValue)
        {
            consulta = consulta.Where(consumo => consumo.OrdenProduccionId == ordenProduccionId.Value);
        }

        if (componenteProductoId.HasValue)
        {
            consulta = consulta.Where(consumo => consumo.ComponenteProductoId == componenteProductoId.Value);
        }

        if (estadoNormalizado != null)
        {
            consulta = consulta.Where(consumo => consumo.Estado == estadoNormalizado);
        }

        // PATRÓN LINQ 6: filtro por rango de fecha (días de Perú convertidos a límites UTC).
        if (desde.HasValue)
        {
            DateTime inicio = ValidacionConsumoMaterial.InicioDelDiaEnPeru(desde.Value);
            consulta = consulta.Where(consumo => consumo.FechaRegistro >= inicio);
        }

        if (hasta.HasValue)
        {
            DateTime limite = ValidacionConsumoMaterial.InicioDelDiaEnPeru(hasta.Value.AddDays(1));
            consulta = consulta.Where(consumo => consumo.FechaRegistro < limite);
        }

        int total = await consulta.CountAsync();
        long inicioPagina = ((long)pagina - 1) * tamanoPagina;
        if (inicioPagina >= total)
        {
            return new PaginaConsumosMaterialesDto(new List<ConsumoMaterialListadoDto>(), total, pagina, tamanoPagina);
        }

        // PATRÓN LINQ 3: proyección directa a DTO.
        List<ConsumoMaterialListadoDto> items = await consulta
            .OrderByDescending(consumo => consumo.FechaRegistro)
            .ThenByDescending(consumo => consumo.Id)
            .Skip((int)inicioPagina)
            .Take(tamanoPagina)
            .Select(consumo => new ConsumoMaterialListadoDto(
                consumo.Id,
                consumo.Numero,
                consumo.OrdenProduccionId,
                consumo.OrdenProduccion.Numero,
                consumo.ComponenteProductoId,
                consumo.ComponenteProducto.Nombre,
                consumo.Cantidad,
                consumo.UnidadMedida,
                consumo.FechaRegistro,
                consumo.Estado))
            .ToListAsync();
        return new PaginaConsumosMaterialesDto(items, total, pagina, tamanoPagina);
    }
}
