using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class ListarReglasReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarReglasReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaReglasReabastecimientoDto> ExecuteAsync(
        int? almacenId,
        int? productoId,
        bool? activa,
        bool? soloBajoPuntoReorden,
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

        IQueryable<ReglaReabastecimiento> consulta = _unitOfWork.Repository<ReglaReabastecimiento>()
            .AsQueryable()
            .AsNoTracking()
            .Include(registro => registro.Producto)
            .Include(registro => registro.Almacen)
            .Include(registro => registro.ProveedorPreferido);

        if (almacenId.HasValue)
        {
            consulta = consulta.Where(registro => registro.AlmacenId == almacenId.Value);
        }

        if (productoId.HasValue)
        {
            consulta = consulta.Where(registro => registro.ProductoId == productoId.Value);
        }

        if (activa.HasValue)
        {
            consulta = consulta.Where(registro => registro.Activa == activa.Value);
        }

        if (soloBajoPuntoReorden == true)
        {
            consulta = consulta.Where(registro => registro.Activa && registro.StockActual <= registro.PuntoReorden);
        }

        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(registro =>
                registro.Codigo.ToLower().Contains(termino) ||
                registro.Producto.Nombre.ToLower().Contains(termino) ||
                registro.Almacen.Codigo.ToLower().Contains(termino) ||
                registro.Almacen.Nombre.ToLower().Contains(termino));
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;

        List<ReglaReabastecimiento> entidades = await consulta
            .OrderBy(registro => registro.Codigo)
            .ThenBy(registro => registro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .ToListAsync();

        List<ReglaReabastecimientoListadoDto> items = entidades
            .Select(ValidacionReabastecimiento.MapearListado)
            .ToList();

        return new PaginaReglasReabastecimientoDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
