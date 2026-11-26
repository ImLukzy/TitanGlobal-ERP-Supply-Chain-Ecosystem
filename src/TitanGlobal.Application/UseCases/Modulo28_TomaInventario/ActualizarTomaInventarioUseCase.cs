using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

public class ActualizarTomaInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarTomaInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TomaInventarioDetalleDto> ExecuteAsync(int id, ActualizarTomaInventarioRequestDto solicitud)
    {
        TomaInventario? tomaEncontrada = await _unitOfWork.Repository<TomaInventario>()
            .AsQueryable()
            .Include(registro => registro.Almacen)
            .Include(registro => registro.DetallesTomaInventario)
                .ThenInclude(detalle => detalle.Producto)
            .SingleOrDefaultAsync(registro => registro.Id == id);
        TomaInventario toma = ValidacionTomaInventario.ValidarTomaExistente(tomaEncontrada, id);
        ValidacionTomaInventario.ValidarEstadoAbierta(toma);

        string responsable = ValidacionTomaInventario.NormalizarResponsable(solicitud.Responsable);
        string? observaciones = ValidacionTomaInventario.NormalizarObservaciones(solicitud.Observaciones);
        List<LineaTomaInventarioRequestDto> lineasSolicitud = ValidacionTomaInventario.ValidarLineas(solicitud.Lineas);

        List<int> productoIds = lineasSolicitud.Select(linea => linea.ProductoId).ToList();
        Dictionary<int, Producto> productos = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(registro => productoIds.Contains(registro.Id))
            .ToDictionaryAsync(registro => registro.Id);

        List<DetalleTomaInventario> detallesAnteriores = toma.DetallesTomaInventario.ToList();
        foreach (DetalleTomaInventario detalleAnterior in detallesAnteriores)
        {
            _unitOfWork.Repository<DetalleTomaInventario>().Remove(detalleAnterior);
        }

        toma.DetallesTomaInventario.Clear();
        toma.Responsable = responsable;
        toma.Observaciones = observaciones;

        foreach (LineaTomaInventarioRequestDto linea in lineasSolicitud)
        {
            productos.TryGetValue(linea.ProductoId, out Producto? productoEncontrado);
            Producto producto = ValidacionKardex.ValidarProductoExistente(productoEncontrado, linea.ProductoId);

            int stockSistema = await ObtenerStockSistemaAsync(producto.Id, toma.AlmacenId);
            DetalleTomaInventario detalle = new DetalleTomaInventario
            {
                TomaInventarioId = toma.Id,
                ProductoId = producto.Id,
                Producto = producto,
                StockSistema = stockSistema,
                CantidadContada = linea.CantidadContada,
                Diferencia = linea.CantidadContada - stockSistema,
                Observacion = ValidacionTomaInventario.NormalizarObservacionLinea(linea.Observacion)
            };
            toma.DetallesTomaInventario.Add(detalle);
        }

        _unitOfWork.Repository<TomaInventario>().Update(toma);
        await _unitOfWork.CompleteAsync();

        return ValidacionTomaInventario.MapearDetalle(toma);
    }

    private async Task<int> ObtenerStockSistemaAsync(int productoId, int almacenId)
    {
        List<MovimientoKardex> movimientos = await _unitOfWork.Repository<MovimientoKardex>()
            .AsQueryable()
            .Where(registro => registro.ProductoId == productoId && registro.AlmacenId == almacenId)
            .OrderBy(registro => registro.Fecha)
            .ThenBy(registro => registro.Id)
            .ToListAsync();

        List<LineaKardexDto> lineas = CalculoKardex.CalcularPromedio(movimientos);
        return lineas.Count > 0 ? lineas[lineas.Count - 1].SaldoCantidad : 0;
    }
}
