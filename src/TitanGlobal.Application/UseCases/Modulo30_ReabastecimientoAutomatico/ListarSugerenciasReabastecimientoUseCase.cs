using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class ListarSugerenciasReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarSugerenciasReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaSugerenciasReabastecimientoDto> ExecuteAsync(
        int? almacenId,
        string? estado,
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

        string? estadoNormalizado = ValidacionReabastecimiento.NormalizarFiltroEstadoSugerencia(estado);

        IQueryable<EjecucionReabastecimiento> consulta = _unitOfWork.Repository<EjecucionReabastecimiento>()
            .AsQueryable()
            .AsNoTracking()
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.Producto)
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.Almacen)
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.ProveedorPreferido)
            .Include(registro => registro.SolicitudCompra);

        if (almacenId.HasValue)
        {
            consulta = consulta.Where(registro => registro.ReglaReabastecimiento.AlmacenId == almacenId.Value);
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(registro => registro.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;

        List<EjecucionReabastecimiento> entidades = await consulta
            .OrderByDescending(registro => registro.FechaEjecucion)
            .ThenByDescending(registro => registro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .ToListAsync();

        List<EjecucionReabastecimientoItemDto> items = entidades
            .Select(ValidacionReabastecimiento.MapearEjecucion)
            .ToList();

        return new PaginaSugerenciasReabastecimientoDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
