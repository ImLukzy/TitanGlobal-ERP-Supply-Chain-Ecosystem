using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

public class RegistrarMovimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarMovimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarMovimientoRequestDto solicitud)
    {
        Producto? productoEncontrado = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProductoId);
        Producto producto = ValidacionKardex.ValidarProductoExistente(productoEncontrado, solicitud.ProductoId);
        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.AlmacenId);
        Almacen almacen = ValidacionKardex.ValidarAlmacenExistente(almacenEncontrado, solicitud.AlmacenId);
        ValidacionKardex.ValidarAlmacenActivo(almacen);
        string tipo = ValidacionKardex.NormalizarTipo(solicitud.Tipo);
        int cantidad = ValidacionKardex.ValidarCantidad(solicitud.Cantidad);
        decimal? costoUnitario = ValidacionKardex.ValidarCostoUnitario(tipo, solicitud.CostoUnitario);
        DateTime fecha = ValidacionKardex.NormalizarFecha(solicitud.Fecha);
        string? referencia = ValidacionKardex.NormalizarReferencia(solicitud.Referencia);

        if (solicitud.LoteId.HasValue)
        {
            int loteId = solicitud.LoteId.Value;
            Lote? lote = await _unitOfWork.Repository<Lote>()
                .AsQueryable()
                .SingleOrDefaultAsync(registro => registro.Id == loteId);
            if (lote is null)
            {
                throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"El lote {loteId} no existe.");
            }

            if (lote.ProductoId != producto.Id)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict,
                    $"El lote {loteId} no pertenece al producto {producto.Id}.");
            }
        }

        if (tipo == "Salida")
        {
            IQueryable<MovimientoKardex> existentes = _unitOfWork.Repository<MovimientoKardex>()
                .AsQueryable()
                .Where(registro => registro.ProductoId == producto.Id && registro.AlmacenId == almacen.Id);
            long entradas = await existentes
                .Where(registro => registro.Tipo == "Entrada")
                .SumAsync(registro => (long?)registro.Cantidad) ?? 0L;
            long salidas = await existentes
                .Where(registro => registro.Tipo == "Salida")
                .SumAsync(registro => (long?)registro.Cantidad) ?? 0L;
            long saldo = entradas - salidas;
            if (cantidad > saldo)
            {
                throw ValidacionKardex.CrearStockInsuficiente(saldo, cantidad);
            }
        }

        MovimientoKardex movimientoNuevo = new MovimientoKardex
        {
            ProductoId = producto.Id,
            AlmacenId = almacen.Id,
            LoteId = solicitud.LoteId,
            Fecha = fecha,
            Tipo = tipo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Referencia = referencia
        };
        await _unitOfWork.Repository<MovimientoKardex>().AddAsync(movimientoNuevo);
        await _unitOfWork.CompleteAsync();

        return movimientoNuevo.Id;
    }
}
