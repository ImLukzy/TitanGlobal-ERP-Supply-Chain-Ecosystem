using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class ListarSeriesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarSeriesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaSeriesDto> ExecuteAsync(
        int? productoId,
        int? loteId,
        string? estado,
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

        IQueryable<UnidadSerie> consulta = _unitOfWork.Repository<UnidadSerie>().AsQueryable();
        if (productoId.HasValue)
        {
            consulta = consulta.Where(registro => registro.ProductoId == productoId.Value);
        }

        if (loteId.HasValue)
        {
            consulta = consulta.Where(registro => registro.LoteId == loteId.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            string estadoNormalizado = ValidacionLoteSerie.NormalizarEstadoSerie(estado);
            consulta = consulta.Where(registro => registro.Estado == estadoNormalizado);
        }

        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(registro => registro.NumeroSerie.ToLower().Contains(busquedaNormalizada));
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<SerieListadoDto> items = await consulta
            .OrderBy(registro => registro.ProductoId)
            .ThenBy(registro => registro.NumeroSerie)
            .ThenBy(registro => registro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(registro => new SerieListadoDto
            {
                Id = registro.Id,
                ProductoId = registro.ProductoId,
                LoteId = registro.LoteId,
                NumeroSerie = registro.NumeroSerie,
                Estado = registro.Estado
            })
            .ToListAsync();

        return new PaginaSeriesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
