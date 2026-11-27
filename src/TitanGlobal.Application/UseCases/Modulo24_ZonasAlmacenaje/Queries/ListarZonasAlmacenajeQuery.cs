using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje.Queries;

public class ListarZonasAlmacenajeQuery : IRequest<PaginaZonasDto>
{
    public int? AlmacenId { get; set; }

    public string? Busqueda { get; set; }

    public string? Tipo { get; set; }

    public string? Temperatura { get; set; }

    public bool? Activa { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarZonasAlmacenajeQueryHandler : IRequestHandler<ListarZonasAlmacenajeQuery, PaginaZonasDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarZonasAlmacenajeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaZonasDto> Handle(ListarZonasAlmacenajeQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<ZonaAlmacenaje> consulta = _unitOfWork.Repository<ZonaAlmacenaje>().AsQueryable();
        if (request.AlmacenId.HasValue)
        {
            consulta = consulta.Where(zona => zona.AlmacenId == request.AlmacenId.Value);
        }

        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(zona =>
                zona.Codigo.ToLower().Contains(busquedaNormalizada)
                || zona.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        if (!string.IsNullOrWhiteSpace(request.Tipo))
        {
            string tipoNormalizado = ValidacionZonaAlmacenaje.NormalizarTipo(request.Tipo);
            consulta = consulta.Where(zona => zona.Tipo == tipoNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(request.Temperatura))
        {
            string temperaturaNormalizada = ValidacionZonaAlmacenaje.NormalizarTemperatura(request.Temperatura);
            consulta = consulta.Where(zona => zona.Temperatura == temperaturaNormalizada);
        }

        if (request.Activa.HasValue)
        {
            consulta = consulta.Where(zona => zona.Activa == request.Activa.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ZonaListadoDto> items = await consulta
            .OrderBy(zona => zona.Almacen.Codigo)
            .ThenBy(zona => zona.Codigo)
            .ThenBy(zona => zona.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
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
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
