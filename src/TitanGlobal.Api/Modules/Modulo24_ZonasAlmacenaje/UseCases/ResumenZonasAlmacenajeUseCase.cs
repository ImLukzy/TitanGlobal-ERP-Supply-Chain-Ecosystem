using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.UseCases;

public class ResumenZonasAlmacenajeUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenZonasAlmacenajeUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenZonasDto> ExecuteAsync(int? almacenId)
    {
        if (almacenId.HasValue)
        {
            bool almacenExiste = await _unitOfWork.Repository<Almacen>()
                .AsQueryable()
                .AnyAsync(almacen => almacen.Id == almacenId.Value);
            if (!almacenExiste)
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status400BadRequest,
                    $"El almacén {almacenId.Value} no existe.");
            }
        }

        IQueryable<ZonaAlmacenaje> zonas = _unitOfWork.Repository<ZonaAlmacenaje>().AsQueryable();
        IQueryable<Almacen> almacenes = _unitOfWork.Repository<Almacen>().AsQueryable();
        if (almacenId.HasValue)
        {
            zonas = zonas.Where(zona => zona.AlmacenId == almacenId.Value);
            almacenes = almacenes.Where(almacen => almacen.Id == almacenId.Value);
        }

        int totalZonas = await zonas.CountAsync();
        int activas = await zonas.CountAsync(zona => zona.Activa);
        List<TipoZonaDto> porTipo = await zonas
            .GroupBy(zona => zona.Tipo)
            .Select(grupo => new TipoZonaDto
            {
                Tipo = grupo.Key,
                TotalZonas = grupo.Count(),
                CapacidadM3 = grupo.Sum(zona => zona.CapacidadM3)
            })
            .OrderBy(tipo => tipo.Tipo == "Recepcion" ? 0
                : tipo.Tipo == "Almacenamiento" ? 1
                : tipo.Tipo == "Picking" ? 2
                : tipo.Tipo == "Despacho" ? 3
                : 4)
            .ToListAsync();
        // PATRÓN LINQ 9: agrupación y orden explícito.

        List<OcupacionAlmacenDto> ocupacion = await almacenes
            .OrderBy(almacen => almacen.Codigo)
            .Select(almacen => new OcupacionAlmacenDto
            {
                AlmacenId = almacen.Id,
                AlmacenCodigo = almacen.Codigo,
                CapacidadAlmacenM3 = almacen.CapacidadM3
            })
            .ToListAsync();
        List<OcupacionAlmacenDto> capacidadesActivas = await zonas
            .Where(zona => zona.Activa)
            .GroupBy(zona => zona.AlmacenId)
            .Select(grupo => new OcupacionAlmacenDto
            {
                AlmacenId = grupo.Key,
                CapacidadAsignadaM3 = grupo.Sum(zona => zona.CapacidadM3)
            })
            .ToListAsync();
        Dictionary<int, decimal> capacidadPorAlmacen = capacidadesActivas
            .ToDictionary(item => item.AlmacenId, item => item.CapacidadAsignadaM3);
        foreach (OcupacionAlmacenDto item in ocupacion)
        {
            capacidadPorAlmacen.TryGetValue(item.AlmacenId, out decimal capacidadAsignadaM3);
            item.CapacidadAsignadaM3 = capacidadAsignadaM3;
            if (capacidadAsignadaM3 == 0 || item.CapacidadAlmacenM3 == 0)
            {
                item.PorcentajeAsignado = 0m;
            }
            else
            {
                item.PorcentajeAsignado = decimal.Round(
                    capacidadAsignadaM3 / item.CapacidadAlmacenM3 * 100m,
                    2,
                    MidpointRounding.AwayFromZero);
            }
        }

        return new ResumenZonasDto
        {
            TotalZonas = totalZonas,
            Activas = activas,
            PorTipo = porTipo,
            Ocupacion = ocupacion
        };
    }
}
