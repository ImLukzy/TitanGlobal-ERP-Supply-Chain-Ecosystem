using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class ObtenerReglaReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerReglaReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReglaReabastecimientoDetalleDto> ExecuteAsync(int id)
    {
        ReglaReabastecimiento? reglaEncontrada = await _unitOfWork.Repository<ReglaReabastecimiento>()
            .AsQueryable()
            .AsNoTracking()
            .Include(registro => registro.Producto)
            .Include(registro => registro.Almacen)
            .Include(registro => registro.ProveedorPreferido)
            .Include(registro => registro.EjecucionesReabastecimiento)
                .ThenInclude(ejecucion => ejecucion.SolicitudCompra)
            .SingleOrDefaultAsync(registro => registro.Id == id);

        ReglaReabastecimiento regla = ValidacionReabastecimiento.ValidarReglaExistente(reglaEncontrada, id);
        ReglaReabastecimientoListadoDto baseDto = ValidacionReabastecimiento.MapearListado(regla);

        List<EjecucionReabastecimientoItemDto> recientes = regla.EjecucionesReabastecimiento
            .OrderByDescending(ejecucion => ejecucion.FechaEjecucion)
            .ThenByDescending(ejecucion => ejecucion.Id)
            .Take(10)
            .Select(ejecucion =>
            {
                ejecucion.ReglaReabastecimiento = regla;
                return ValidacionReabastecimiento.MapearEjecucion(ejecucion);
            })
            .ToList();

        return new ReglaReabastecimientoDetalleDto
        {
            Id = baseDto.Id,
            Codigo = baseDto.Codigo,
            ProductoId = baseDto.ProductoId,
            ProductoNombre = baseDto.ProductoNombre,
            ProductoPrecio = baseDto.ProductoPrecio,
            AlmacenId = baseDto.AlmacenId,
            AlmacenCodigo = baseDto.AlmacenCodigo,
            AlmacenNombre = baseDto.AlmacenNombre,
            ProveedorPreferidoId = baseDto.ProveedorPreferidoId,
            ProveedorPreferidoNombre = baseDto.ProveedorPreferidoNombre,
            StockActual = baseDto.StockActual,
            StockMinimo = baseDto.StockMinimo,
            PuntoReorden = baseDto.PuntoReorden,
            StockMaximo = baseDto.StockMaximo,
            CantidadLoteCompra = baseDto.CantidadLoteCompra,
            CantidadSugeridaActual = baseDto.CantidadSugeridaActual,
            RequiereReabastecimiento = baseDto.RequiereReabastecimiento,
            Activa = baseDto.Activa,
            UltimaEvaluacion = baseDto.UltimaEvaluacion,
            Observaciones = regla.Observaciones,
            EjecucionesRecientes = recientes
        };
    }
}
