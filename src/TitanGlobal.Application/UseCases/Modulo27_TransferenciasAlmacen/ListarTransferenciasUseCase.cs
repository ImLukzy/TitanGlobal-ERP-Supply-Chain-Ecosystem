using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo27_TransferenciasAlmacen;

public class ListarTransferenciasUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarTransferenciasUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaTransferenciasDto> ExecuteAsync(
        int? productoId,
        int? almacenOrigenId,
        int? almacenDestinoId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value.Date > fechaHasta.Value.Date)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha inicial no puede ser posterior a la final.");
        }

        IQueryable<TransferenciaAlmacen> consulta = _unitOfWork.Repository<TransferenciaAlmacen>().AsQueryable();
        if (productoId.HasValue)
        {
            consulta = consulta.Where(registro => registro.ProductoId == productoId.Value);
        }

        if (almacenOrigenId.HasValue)
        {
            consulta = consulta.Where(registro => registro.AlmacenOrigenId == almacenOrigenId.Value);
        }

        if (almacenDestinoId.HasValue)
        {
            consulta = consulta.Where(registro => registro.AlmacenDestinoId == almacenDestinoId.Value);
        }

        if (fechaDesde.HasValue)
        {
            DateTime desde = FechasUtc.Normalizar(DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc));
            consulta = consulta.Where(registro => registro.Fecha >= desde);
        }

        if (fechaHasta.HasValue)
        {
            DateTime hastaExclusivo = FechasUtc.Normalizar(
                DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc));
            consulta = consulta.Where(registro => registro.Fecha < hastaExclusivo);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<TransferenciaListadoDto> items = await consulta
            .OrderByDescending(registro => registro.Fecha)
            .ThenByDescending(registro => registro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(registro => new TransferenciaListadoDto
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
                Fecha = registro.Fecha
            })
            .ToListAsync();

        return new PaginaTransferenciasDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
