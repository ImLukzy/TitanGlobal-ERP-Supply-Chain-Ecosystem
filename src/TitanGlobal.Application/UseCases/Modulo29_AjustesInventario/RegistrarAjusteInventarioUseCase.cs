using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo29_AjustesInventario;

public class RegistrarAjusteInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarAjusteInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AjusteInventarioItemDto> ExecuteAsync(RegistrarAjusteInventarioRequestDto solicitud)
    {
        string codigo = ValidacionAjustesInventario.NormalizarCodigo(solicitud.Codigo);
        string tipo = ValidacionAjustesInventario.NormalizarTipo(solicitud.Tipo);
        string motivo = ValidacionAjustesInventario.NormalizarMotivo(solicitud.Motivo);
        int cantidad = ValidacionKardex.ValidarCantidad(solicitud.Cantidad);
        string responsable = ValidacionAjustesInventario.NormalizarResponsable(solicitud.Responsable);
        string? observaciones = ValidacionAjustesInventario.NormalizarObservaciones(solicitud.Observaciones);
        DateTime fecha = ValidacionKardex.NormalizarFecha(solicitud.Fecha);

        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.AlmacenId);
        Almacen almacen = ValidacionKardex.ValidarAlmacenExistente(almacenEncontrado, solicitud.AlmacenId);
        ValidacionKardex.ValidarAlmacenActivo(almacen);

        Producto? productoEncontrado = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProductoId);
        Producto producto = ValidacionKardex.ValidarProductoExistente(productoEncontrado, solicitud.ProductoId);

        string codigoNormalizado = codigo.ToLower();
        bool codigoDuplicado = await _unitOfWork.Repository<AjusteInventario>()
            .AsQueryable()
            .AnyAsync(registro => registro.Codigo.Trim().ToLower() == codigoNormalizado);
        if (codigoDuplicado)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe un ajuste de inventario con el código {codigo}.");
        }

        List<MovimientoKardex> movimientos = await _unitOfWork.Repository<MovimientoKardex>()
            .AsQueryable()
            .Where(registro => registro.ProductoId == producto.Id && registro.AlmacenId == almacen.Id)
            .OrderBy(registro => registro.Fecha)
            .ThenBy(registro => registro.Id)
            .ToListAsync();

        (int saldoCantidad, decimal saldoValor) = CalcularSaldoActual(movimientos);
        decimal costoUnitarioFinal = ResolverCostoUnitario(
            tipo,
            cantidad,
            solicitud.CostoUnitario,
            saldoCantidad,
            saldoValor,
            producto.Precio);

        AjusteInventario ajuste = new AjusteInventario
        {
            Codigo = codigo,
            AlmacenId = almacen.Id,
            ProductoId = producto.Id,
            Tipo = tipo,
            Motivo = motivo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitarioFinal,
            Fecha = fecha,
            Responsable = responsable,
            Observaciones = observaciones,
            Almacen = almacen,
            Producto = producto
        };

        MovimientoKardex movimiento = new MovimientoKardex
        {
            ProductoId = producto.Id,
            AlmacenId = almacen.Id,
            Fecha = fecha,
            Tipo = tipo == "Incremento" ? "Entrada" : "Salida",
            Cantidad = cantidad,
            CostoUnitario = tipo == "Incremento" ? costoUnitarioFinal : null,
            Referencia = $"AJUSTE {codigo}",
            AjusteInventario = ajuste
        };

        await _unitOfWork.Repository<AjusteInventario>().AddAsync(ajuste);
        await _unitOfWork.Repository<MovimientoKardex>().AddAsync(movimiento);
        await _unitOfWork.CompleteAsync();

        return ValidacionAjustesInventario.MapearItem(ajuste);
    }

    private static (int SaldoCantidad, decimal SaldoValor) CalcularSaldoActual(List<MovimientoKardex> movimientos)
    {
        int saldoCantidad = 0;
        decimal saldoValor = 0m;
        foreach (MovimientoKardex movimiento in movimientos)
        {
            if (movimiento.Tipo == "Entrada")
            {
                decimal costoEntrada = movimiento.CostoUnitario ?? 0m;
                saldoCantidad += movimiento.Cantidad;
                saldoValor += movimiento.Cantidad * costoEntrada;
            }
            else if (saldoCantidad <= 0)
            {
                saldoCantidad -= movimiento.Cantidad;
            }
            else
            {
                decimal costoSalida = saldoValor / saldoCantidad;
                decimal valorSalida = movimiento.Cantidad >= saldoCantidad
                    ? saldoValor
                    : movimiento.Cantidad * costoSalida;
                saldoCantidad -= movimiento.Cantidad;
                saldoValor = saldoCantidad <= 0 ? 0m : saldoValor - valorSalida;
            }
        }

        return (saldoCantidad, saldoValor);
    }

    private static decimal ResolverCostoUnitario(
        string tipo,
        int cantidad,
        decimal? costoSolicitado,
        int saldoCantidad,
        decimal saldoValor,
        decimal precioProducto)
    {
        if (tipo == "Decremento")
        {
            if (costoSolicitado.HasValue)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "Los ajustes de decremento se valorizan automáticamente al costo promedio del Kardex y no reciben costo unitario.");
            }

            if (cantidad > saldoCantidad)
            {
                throw ValidacionKardex.CrearStockInsuficiente(saldoCantidad, cantidad);
            }

            if (saldoCantidad > 0 && saldoValor > 0m)
            {
                return Math.Round(saldoValor / saldoCantidad, 4);
            }

            return precioProducto > 0m ? Math.Round(precioProducto, 4) : 1m;
        }

        if (costoSolicitado.HasValue)
        {
            return ValidacionAjustesInventario.ValidarCostoIncrementoExplicito(costoSolicitado.Value);
        }

        if (saldoCantidad > 0 && saldoValor > 0m)
        {
            return Math.Round(saldoValor / saldoCantidad, 4);
        }

        if (precioProducto > 0m)
        {
            return Math.Round(precioProducto, 4);
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "Debe indicar el costo unitario cuando el producto no tiene saldo valorizado previo en el almacén.");
    }
}
