using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

public class ListarVehiculosFlotaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarVehiculosFlotaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaVehiculosFlotaDto> ExecuteAsync(
        string? busqueda,
        string? tipo,
        bool? activo,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El número de página debe ser mayor que cero.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        // PATRÓN LINQ 1: filtro de texto insensible a mayúsculas
        IQueryable<VehiculoFlota> consulta = _unitOfWork.Repository<VehiculoFlota>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLower();
            consulta = consulta.Where(v =>
                v.Placa.ToLower().Contains(termino) ||
                v.Marca.ToLower().Contains(termino) ||
                v.Modelo.ToLower().Contains(termino));
        }

        // PATRÓN LINQ 2: filtro exacto insensible a mayúsculas
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string tipoFiltro = tipo.Trim().ToLower();
            consulta = consulta.Where(v => v.Tipo.ToLower() == tipoFiltro);
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(v => v.Activo == activo.Value);
        }

        int total = await consulta.CountAsync();

        // PATRÓN LINQ 3: proyección directa a DTO
        List<VehiculoFlotaListadoDto> items = await consulta
            .OrderBy(v => v.Placa)
            .ThenBy(v => v.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
