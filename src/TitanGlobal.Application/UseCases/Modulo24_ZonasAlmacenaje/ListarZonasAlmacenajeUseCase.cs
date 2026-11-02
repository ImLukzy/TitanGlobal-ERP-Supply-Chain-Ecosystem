using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje;

public class ListarZonasAlmacenajeUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarZonasAlmacenajeUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaZonasDto> ExecuteAsync(
        int? almacenId,
        string? busqueda,
        string? tipo,
        string? temperatura,
        bool? activa,
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

        IQueryable<ZonaAlmacenaje> consulta = _unitOfWork.Repository<ZonaAlmacenaje>().AsQueryable();
        if (almacenId.HasValue)
        {
            consulta = consulta.Where(zona => zona.AlmacenId == almacenId.Value);
        }

        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(zona =>
                zona.Codigo.ToLower().Contains(busquedaNormalizada)
                || zona.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string tipoNormalizado = ValidacionZonaAlmacenaje.NormalizarTipo(tipo);
            consulta = consulta.Where(zona => zona.Tipo == tipoNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(temperatura))
        {
            string temperaturaNormalizada = ValidacionZonaAlmacenaje.NormalizarTemperatura(temperatura);
            consulta = consulta.Where(zona => zona.Temperatura == temperaturaNormalizada);
        }

        if (activa.HasValue)
        {
            consulta = consulta.Where(zona => zona.Activa == activa.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ZonaListadoDto> items = await consulta
            .OrderBy(zona => zona.Almacen.Codigo)
            .ThenBy(zona => zona.Codigo)
            .ThenBy(zona => zona.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(zona => new ZonaListadoDto
            {
                Id = zona.Id,
                AlmacenId = zona.AlmacenId,
                AlmacenCodigo = zona.Almacen.Codigo,
                Codigo = zona.Codigo,
                Nombre = zona.Nombre,
                Tipo = zona.Tipo,
                Temperatura = zona.Temperatura,
                CapacidadM3 = zona.CapacidadM3,
                Activa = zona.Activa
            })
            .ToListAsync();

        return new PaginaZonasDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
