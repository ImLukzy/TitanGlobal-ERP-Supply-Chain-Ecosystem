using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo;

public class ListarCentrosTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCentrosTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaCentrosTrabajoDto> ExecuteAsync(
        string? busqueda,
        string? planta,
        string? tipo,
        bool? activo,
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

        IQueryable<CentroTrabajo> consulta = _unitOfWork.Repository<CentroTrabajo>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(centro =>
                centro.Codigo.ToLower().Contains(busquedaNormalizada)
                || centro.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        string plantaNormalizada = planta?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(plantaNormalizada))
        {
            consulta = consulta.Where(centro => centro.Planta.ToLower() == plantaNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string tipoNormalizado = ValidacionCentroTrabajo.NormalizarTipo(tipo);
            consulta = consulta.Where(centro => centro.Tipo == tipoNormalizado);
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(centro => centro.Activo == activo.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<CentroTrabajoListadoDto> items = await consulta
            .OrderBy(centro => centro.Codigo)
            .ThenBy(centro => centro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(centro => new CentroTrabajoListadoDto
            {
                Id = centro.Id,
                Codigo = centro.Codigo,
                Nombre = centro.Nombre,
                Tipo = centro.Tipo,
                Planta = centro.Planta,
                CapacidadHorasDia = centro.CapacidadHorasDia,
                Activo = centro.Activo
            })
            .ToListAsync();

        return new PaginaCentrosTrabajoDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
