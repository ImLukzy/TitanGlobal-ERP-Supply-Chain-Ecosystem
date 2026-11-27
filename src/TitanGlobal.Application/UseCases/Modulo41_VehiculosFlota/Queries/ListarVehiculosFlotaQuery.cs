using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Queries;

public class ListarVehiculosFlotaQuery : IRequest<PaginaVehiculosFlotaDto>
{
    public string? Busqueda { get; set; }

    public string? Tipo { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarVehiculosFlotaQueryHandler : IRequestHandler<ListarVehiculosFlotaQuery, PaginaVehiculosFlotaDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarVehiculosFlotaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaVehiculosFlotaDto> Handle(ListarVehiculosFlotaQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de página debe ser mayor que cero.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        // PATRÓN LINQ 1: filtro de texto insensible a mayúsculas
        IQueryable<VehiculoFlota> consulta = _unitOfWork.Repository<VehiculoFlota>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            string termino = request.Busqueda.Trim().ToLower();
            consulta = consulta.Where(v =>
                v.Placa.ToLower().Contains(termino) ||
                v.Marca.ToLower().Contains(termino) ||
                v.Modelo.ToLower().Contains(termino));
        }

        // PATRÓN LINQ 2: filtro exacto insensible a mayúsculas
        if (!string.IsNullOrWhiteSpace(request.Tipo))
        {
            string tipoFiltro = request.Tipo.Trim().ToLower();
            consulta = consulta.Where(v => v.Tipo.ToLower() == tipoFiltro);
        }

        if (request.Activo.HasValue)
        {
            consulta = consulta.Where(v => v.Activo == request.Activo.Value);
        }

        int total = await consulta.CountAsync();

        // PATRÓN LINQ 3: proyección directa a DTO
        List<VehiculoFlotaListadoDto> items = await consulta
            .OrderBy(v => v.Placa)
            .ThenBy(v => v.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(v => new VehiculoFlotaListadoDto
            {
                Id = v.Id,
                Placa = v.Placa,
                Marca = v.Marca,
                Modelo = v.Modelo,
                Anio = v.Anio,
                Tipo = v.Tipo,
                CapacidadKg = v.CapacidadKg,
                Activo = v.Activo
            })
            .ToListAsync();

        return new PaginaVehiculosFlotaDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
