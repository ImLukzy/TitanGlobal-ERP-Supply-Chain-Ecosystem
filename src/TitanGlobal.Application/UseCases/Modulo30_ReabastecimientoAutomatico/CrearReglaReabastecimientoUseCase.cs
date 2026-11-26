using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class CrearReglaReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearReglaReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReglaReabastecimientoDetalleDto> ExecuteAsync(CrearReglaReabastecimientoRequestDto solicitud)
    {
        string codigo = ValidacionReabastecimiento.NormalizarCodigo(solicitud.Codigo);
        ValidacionReabastecimiento.ValidarParametrosStock(
            solicitud.StockActual,
            solicitud.StockMinimo,
            solicitud.PuntoReorden,
            solicitud.StockMaximo,
            solicitud.CantidadLoteCompra);
        string? observaciones = ValidacionReabastecimiento.NormalizarObservaciones(solicitud.Observaciones);

        Producto? productoEncontrado = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProductoId);
        Producto producto = ValidacionReabastecimiento.ValidarProductoExistente(productoEncontrado, solicitud.ProductoId);

        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.AlmacenId);
        Almacen almacen = ValidacionReabastecimiento.ValidarAlmacenExistente(almacenEncontrado, solicitud.AlmacenId);
        ValidacionReabastecimiento.ValidarAlmacenActivo(almacen);

        Proveedor? proveedorEncontrado = null;
        if (solicitud.ProveedorPreferidoId.HasValue)
        {
            proveedorEncontrado = await _unitOfWork.Repository<Proveedor>()
                .AsQueryable()
                .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProveedorPreferidoId.Value);
        }
        Proveedor? proveedor = ValidacionReabastecimiento.ValidarProveedorOpcional(
            proveedorEncontrado,
            solicitud.ProveedorPreferidoId);

        string codigoNormalizado = codigo.ToLowerInvariant();
        bool codigoDuplicado = await _unitOfWork.Repository<ReglaReabastecimiento>()
            .AsQueryable()
            .AnyAsync(registro => registro.Codigo.Trim().ToLower() == codigoNormalizado);
        if (codigoDuplicado)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe una regla de reabastecimiento con el código {codigo}.");
        }

        bool combinacionDuplicada = await _unitOfWork.Repository<ReglaReabastecimiento>()
            .AsQueryable()
            .AnyAsync(registro => registro.ProductoId == producto.Id && registro.AlmacenId == almacen.Id);
        if (combinacionDuplicada)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe una regla de reabastecimiento para el producto {producto.Nombre} en el almacén {almacen.Codigo}.");
        }

        ReglaReabastecimiento regla = new ReglaReabastecimiento
        {
            Codigo = codigo,
            ProductoId = producto.Id,
            Producto = producto,
            AlmacenId = almacen.Id,
            Almacen = almacen,
            ProveedorPreferidoId = proveedor?.Id,
            ProveedorPreferido = proveedor,
            StockActual = solicitud.StockActual,
            StockMinimo = solicitud.StockMinimo,
            PuntoReorden = solicitud.PuntoReorden,
            StockMaximo = solicitud.StockMaximo,
            CantidadLoteCompra = solicitud.CantidadLoteCompra,
            Activa = solicitud.Activa,
            Observaciones = observaciones
        };

        await _unitOfWork.Repository<ReglaReabastecimiento>().AddAsync(regla);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException ex) when (ex.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe una regla de reabastecimiento con el código {codigo} o para la misma combinación de producto y almacén.");
        }

        ReglaReabastecimientoListadoDto baseDto = ValidacionReabastecimiento.MapearListado(regla);
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
            EjecucionesRecientes = new List<EjecucionReabastecimientoItemDto>()
        };
    }
}
