using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

public class BalanceConsumoOrdenUseCase
{
    private readonly IUnitOfWork _unidad;

    public BalanceConsumoOrdenUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<BalanceConsumoOrdenDto> ExecuteAsync(int ordenProduccionId)
    {
        OrdenProduccion? orden = await _unidad.Repository<OrdenProduccion>().AsQueryable()
            .Include(existente => existente.Producto)
            .SingleOrDefaultAsync(existente => existente.Id == ordenProduccionId);
        if (orden == null)
        {
            throw ValidacionConsumoMaterial.OrdenNoEncontrada(ordenProduccionId);
        }

        int cantidadPlanificada = orden.CantidadPlanificada;

        // PATRÓN LINQ 9: agrupación por componente con la suma en la consulta.
        Dictionary<int, decimal> consumidos = await _unidad.Repository<ConsumoMaterial>().AsQueryable()
            .Where(consumo => consumo.OrdenProduccionId == ordenProduccionId
                && consumo.Estado == ValidacionConsumoMaterial.Registrado)
            .GroupBy(consumo => consumo.ComponenteProductoId)
            .Select(grupo => new { ComponenteId = grupo.Key, Total = grupo.Sum(consumo => consumo.Cantidad) })
            .ToDictionaryAsync(grupo => grupo.ComponenteId, grupo => grupo.Total);

        // PATRÓN LINQ 10: líneas hijas de la lista fija de la orden, con la cantidad requerida calculada.
        List<LineaBalanceConsumoDto> requeridas = await _unidad.Repository<DetalleListaMaterial>().AsQueryable()
            .Where(detalle => detalle.ListaMaterialId == orden.ListaMaterialesId)
            .OrderBy(detalle => detalle.ComponenteProducto.Nombre)
            .ThenBy(detalle => detalle.ComponenteProductoId)
            .Select(detalle => new LineaBalanceConsumoDto(
                detalle.ComponenteProductoId,
                detalle.ComponenteProducto.Nombre,
                detalle.UnidadMedida,
                detalle.Cantidad * cantidadPlanificada,
                0m,
                0m,
                0m))
            .ToListAsync();

        List<LineaBalanceConsumoDto> lineas = requeridas.Select(linea => Completar(linea, consumidos)).ToList();
        return new BalanceConsumoOrdenDto(
            orden.Id, orden.Numero, orden.Producto.Nombre, orden.CantidadPlanificada, orden.Estado, lineas);
    }

    private static LineaBalanceConsumoDto Completar(LineaBalanceConsumoDto linea, Dictionary<int, decimal> consumidos)
    {
        decimal consumida = consumidos.GetValueOrDefault(linea.ComponenteProductoId);
        decimal requerida = linea.CantidadRequerida;
        decimal pendiente = Math.Max(requerida - consumida, 0m);
        decimal desviacion = Math.Round((consumida - requerida) / requerida * 100m, 2, MidpointRounding.AwayFromZero);
        return linea with
        {
            CantidadConsumida = consumida,
            CantidadPendiente = pendiente,
            DesviacionPorcentaje = desviacion
        };
    }
}
