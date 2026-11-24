using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

public class ListarCentrosCostoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCentrosCostoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaCentrosCostoDto> ExecuteAsync(bool? activo, int? centroCostoPadreId, int pagina, int tamanoPagina)
    {
        if (pagina < 1 || tamanoPagina < 1 || tamanoPagina > 100 || centroCostoPadreId <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, ValidacionCentroCosto.DatosInvalidos);
        }

        IQueryable<CentroCosto> consulta = _unitOfWork.Repository<CentroCosto>().AsQueryable().AsNoTracking();
        if (activo.HasValue)
        {
            consulta = consulta.Where(centro => centro.Activo == activo.Value);
        }
        if (centroCostoPadreId.HasValue)
        {
            consulta = consulta.Where(centro => centro.CentroCostoPadreId == centroCostoPadreId.Value);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        IReadOnlyList<CentroCostoListadoDto> items = Array.Empty<CentroCostoListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderBy(centro => centro.Codigo).ThenBy(centro => centro.Id)
                .Skip((int)desplazamiento).Take(tamanoPagina)
                .Select(centro => new CentroCostoListadoDto
                {
                    Id = centro.Id,
                    Codigo = centro.Codigo,
                    Nombre = centro.Nombre,
                    CentroCostoPadreId = centro.CentroCostoPadreId,
                    ResponsableEmpleadoId = centro.ResponsableEmpleadoId,
                    Activo = centro.Activo
                }).ToListAsync();
        }

        return new PaginaCentrosCostoDto
        {
            Items = items,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling((decimal)total / tamanoPagina)
        };
    }
}
