using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

public class ListarMovimientosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarMovimientosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaMovimientosDto> ExecuteAsync(
        int? productoId,
        int? almacenId,
        string? tipo,
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

        IQueryable<MovimientoKardex> consulta = _unitOfWork.Repository<MovimientoKardex>().AsQueryable();
        if (productoId.HasValue)
        {
            consulta = consulta.Where(registro => registro.ProductoId == productoId.Value);
        }

        if (almacenId.HasValue)
        {
            consulta = consulta.Where(registro => registro.AlmacenId == almacenId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string tipoNormalizado = ValidacionKardex.NormalizarTipo(tipo);
            consulta = consulta.Where(registro => registro.Tipo == tipoNormalizado);
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
        List<MovimientoListadoDto> items = await consulta
            .OrderByDescending(registro => registro.Fecha)
            .ThenByDescending(registro => registro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(registro => new MovimientoListadoDto
            {
                Id = registro.Id,
                ProductoId = registro.ProductoId,
                ProductoNombre = registro.Producto.Nombre,
                AlmacenId = registro.AlmacenId,
                AlmacenCodigo = registro.Almacen.Codigo,
                LoteId = registro.LoteId,
                Fecha = registro.Fecha,
                Tipo = registro.Tipo,
                Cantidad = registro.Cantidad,
                CostoUnitario = registro.CostoUnitario,
                Referencia = registro.Referencia
            })
            .ToListAsync();

        return new PaginaMovimientosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
