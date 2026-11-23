using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

public class CrearTomaInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearTomaInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TomaInventarioDetalleDto> ExecuteAsync(CrearTomaInventarioRequestDto solicitud)
    {
        string codigo = ValidacionTomaInventario.NormalizarCodigo(solicitud.Codigo);
        string responsable = ValidacionTomaInventario.NormalizarResponsable(solicitud.Responsable);
        string? observaciones = ValidacionTomaInventario.NormalizarObservaciones(solicitud.Observaciones);
        DateTime fechaToma = ValidacionTomaInventario.NormalizarFechaToma(solicitud.FechaToma);
        List<LineaTomaInventarioRequestDto> lineasSolicitud = ValidacionTomaInventario.ValidarLineas(solicitud.Lineas);

        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.AlmacenId);
        Almacen almacen = ValidacionKardex.ValidarAlmacenExistente(almacenEncontrado, solicitud.AlmacenId);
        ValidacionKardex.ValidarAlmacenActivo(almacen);

        string codigoNormalizado = codigo.ToLowerInvariant();
        bool codigoDuplicado = await _unitOfWork.Repository<TomaInventario>()
            .AsQueryable()
            .AnyAsync(registro => registro.Codigo.Trim().ToLower() == codigoNormalizado);
        if (codigoDuplicado)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe una toma de inventario con el código {codigo}.");
        }

        List<int> productoIds = lineasSolicitud.Select(linea => linea.ProductoId).ToList();
        Dictionary<int, Producto> productos = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(registro => productoIds.Contains(registro.Id))
            .ToDictionaryAsync(registro => registro.Id);

        TomaInventario toma = new TomaInventario
        {
            Codigo = codigo,
            AlmacenId = almacen.Id,
            Almacen = almacen,
            FechaToma = fechaToma,
            Estado = "Abierta",
            Responsable = responsable,
            Observaciones = observaciones
        };

        foreach (LineaTomaInventarioRequestDto linea in lineasSolicitud)
        {
            productos.TryGetValue(linea.ProductoId, out Producto? productoEncontrado);
            Producto producto = ValidacionKardex.ValidarProductoExistente(productoEncontrado, linea.ProductoId);

            int stockSistema = await ObtenerStockSistemaAsync(producto.Id, almacen.Id);
            DetalleTomaInventario detalle = new DetalleTomaInventario
            {
                ProductoId = producto.Id,
                Producto = producto,
                StockSistema = stockSistema,
                CantidadContada = linea.CantidadContada,
                Diferencia = linea.CantidadContada - stockSistema,
                Observacion = ValidacionTomaInventario.NormalizarObservacionLinea(linea.Observacion)
            };
            toma.DetallesTomaInventario.Add(detalle);
        }

        await _unitOfWork.Repository<TomaInventario>().AddAsync(toma);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException ex) when (ex.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe una toma de inventario con el código {codigo}.");
        }

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
