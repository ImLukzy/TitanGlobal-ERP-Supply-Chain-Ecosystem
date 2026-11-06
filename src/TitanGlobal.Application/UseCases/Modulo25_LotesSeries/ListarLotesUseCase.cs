using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class ListarLotesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarLotesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaLotesDto> ExecuteAsync(
        int? productoId,
        string? estado,
        bool? vencidos,
        string? busqueda,
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

        IQueryable<Lote> consulta = _unitOfWork.Repository<Lote>().AsQueryable();
        if (productoId.HasValue)
        {
            consulta = consulta.Where(registro => registro.ProductoId == productoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            string estadoNormalizado = ValidacionLoteSerie.NormalizarEstadoLote(estado);
            consulta = consulta.Where(registro => registro.Estado == estadoNormalizado);
        }

        DateTime hoy = DateTime.UtcNow.Date;
        if (vencidos.HasValue)
        {
            if (vencidos.Value)
            {
                consulta = consulta.Where(registro => registro.FechaVencimiento != null && registro.FechaVencimiento < hoy);
            }
            else
            {
                consulta = consulta.Where(registro => registro.FechaVencimiento == null || registro.FechaVencimiento >= hoy);
            }
        }

        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(registro => registro.Codigo.ToLower().Contains(busquedaNormalizada));
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<LoteListadoDto> items = await consulta
            .OrderBy(registro => registro.ProductoId)
            .ThenBy(registro => registro.Codigo)
            .ThenBy(registro => registro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(registro => new LoteListadoDto
            {
                Id = registro.Id,
                ProductoId = registro.ProductoId,
                ProductoNombre = registro.Producto.Nombre,
                Codigo = registro.Codigo,
                FechaVencimiento = registro.FechaVencimiento,
                Cantidad = registro.Cantidad,
                Estado = registro.Estado,
                Vencido = registro.FechaVencimiento != null && registro.FechaVencimiento < hoy
            })
            .ToListAsync();

        return new PaginaLotesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
