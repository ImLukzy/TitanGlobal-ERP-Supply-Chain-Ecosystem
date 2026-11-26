using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo27_TransferenciasAlmacen;

public class ObtenerTransferenciaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerTransferenciaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TransferenciaDetalleDto> ExecuteAsync(int id)
    {
        TransferenciaDetalleDto? transferencia = await _unitOfWork.Repository<TransferenciaAlmacen>()
            .AsQueryable()
            .Where(registro => registro.Id == id)
            .Select(registro => new TransferenciaDetalleDto
            {
                Id = registro.Id,
                ProductoId = registro.ProductoId,
                ProductoNombre = registro.Producto.Nombre,
                AlmacenOrigenId = registro.AlmacenOrigenId,
                AlmacenOrigenCodigo = registro.AlmacenOrigen.Codigo,
                AlmacenDestinoId = registro.AlmacenDestinoId,
                AlmacenDestinoCodigo = registro.AlmacenDestino.Codigo,
                Cantidad = registro.Cantidad,
                CostoUnitario = registro.CostoUnitario,
                ValorTotal = registro.Cantidad * registro.CostoUnitario,
                Fecha = registro.Fecha,
                Observaciones = registro.Observaciones,
                MovimientoSalidaId = registro.MovimientosKardex.Where(movimiento => movimiento.Tipo == "Salida").Select(movimiento => movimiento.Id).FirstOrDefault(),
                MovimientoEntradaId = registro.MovimientosKardex.Where(movimiento => movimiento.Tipo == "Entrada").Select(movimiento => movimiento.Id).FirstOrDefault()
            })
            .SingleOrDefaultAsync();
        if (transferencia is null)
        {
            throw ValidacionTransferencia.CrearNoEncontrada(id);
        }

        return transferencia;
    }
}
