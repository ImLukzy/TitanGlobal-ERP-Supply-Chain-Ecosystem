using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;
using TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo27_TransferenciasAlmacen;

public class RegistrarTransferenciaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarTransferenciaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TransferenciaCreadaDto> ExecuteAsync(GuardarTransferenciaRequestDto solicitud)
    {
        Producto? productoEncontrado = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProductoId);
        Producto producto = ValidacionKardex.ValidarProductoExistente(productoEncontrado, solicitud.ProductoId);
        Almacen? origenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.AlmacenOrigenId);
        Almacen origen = ValidacionKardex.ValidarAlmacenExistente(origenEncontrado, solicitud.AlmacenOrigenId);
        Almacen? destinoEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.AlmacenDestinoId);
        Almacen destino = ValidacionKardex.ValidarAlmacenExistente(destinoEncontrado, solicitud.AlmacenDestinoId);
        ValidacionTransferencia.ValidarAlmacenesDistintos(origen.Id, destino.Id);
        ValidacionKardex.ValidarAlmacenActivo(origen);
        ValidacionKardex.ValidarAlmacenActivo(destino);
        int cantidad = ValidacionTransferencia.ValidarCantidad(solicitud.Cantidad);
        string? observaciones = ValidacionTransferencia.NormalizarObservaciones(solicitud.Observaciones);

        List<MovimientoKardex> movimientosOrigen = await _unitOfWork.Repository<MovimientoKardex>()
            .AsQueryable()
            .Where(registro => registro.ProductoId == producto.Id && registro.AlmacenId == origen.Id)
            .OrderBy(registro => registro.Fecha)
            .ThenBy(registro => registro.Id)
            .ToListAsync();
        List<LineaKardexDto> lineas = CalculoKardex.CalcularPromedio(movimientosOrigen);
        int saldo = 0;
        decimal costoUnitario = 0m;
        if (lineas.Count > 0)
        {
            LineaKardexDto ultima = lineas[lineas.Count - 1];
            saldo = ultima.SaldoCantidad;
            costoUnitario = ultima.CostoPromedio;
        }

        if (cantidad > saldo)
        {
            throw ValidacionKardex.CrearStockInsuficiente(saldo, cantidad);
        }

        DateTime fecha = DateTime.UtcNow;
        TransferenciaAlmacen transferencia = new TransferenciaAlmacen
        {
            ProductoId = producto.Id,
            AlmacenOrigenId = origen.Id,
            AlmacenDestinoId = destino.Id,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Fecha = fecha,
            Observaciones = observaciones
        };
        MovimientoKardex salida = new MovimientoKardex
        {
            ProductoId = producto.Id,
            AlmacenId = origen.Id,
            Fecha = fecha,
            Tipo = "Salida",
            Cantidad = cantidad,
            CostoUnitario = null,
            Referencia = $"Transferencia hacia {destino.Codigo}",
            Transferencia = transferencia
        };
        MovimientoKardex entrada = new MovimientoKardex
        {
            ProductoId = producto.Id,
            AlmacenId = destino.Id,
            Fecha = fecha,
            Tipo = "Entrada",
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            Referencia = $"Transferencia desde {origen.Codigo}",
            Transferencia = transferencia
        };
        await _unitOfWork.Repository<TransferenciaAlmacen>().AddAsync(transferencia);
        await _unitOfWork.Repository<MovimientoKardex>().AddAsync(salida);
        await _unitOfWork.Repository<MovimientoKardex>().AddAsync(entrada);
        await _unitOfWork.CompleteAsync();

        return new TransferenciaCreadaDto
        {
            Id = transferencia.Id,
            CostoUnitario = costoUnitario
        };
    }
}
