using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo23_Almacenes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.UseCases;

public class ResumenAlmacenesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenAlmacenesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenAlmacenesDto> ExecuteAsync()
    {
        IQueryable<Almacen> almacenes = _unitOfWork.Repository<Almacen>().AsQueryable();
        int totalAlmacenes = await almacenes.CountAsync();
        int activos = await almacenes.CountAsync(almacen => almacen.Activo);
        decimal capacidadTotalM3 = await almacenes.SumAsync(almacen => almacen.CapacidadM3);
        decimal capacidadActivaM3 = await almacenes
            .Where(almacen => almacen.Activo)
            .SumAsync(almacen => almacen.CapacidadM3);
        List<TipoAlmacenesDto> porTipo = await almacenes
            .GroupBy(almacen => almacen.Tipo)
            .Select(grupo => new TipoAlmacenesDto
            {
                Tipo = grupo.Key,
                TotalAlmacenes = grupo.Count(),
                CapacidadM3 = grupo.Sum(almacen => almacen.CapacidadM3)
            })
            .OrderBy(tipo => tipo.Tipo == "Central" ? 0 : tipo.Tipo == "Regional" ? 1 : 2)
            .ToListAsync();
        // PATRÓN LINQ 9: agrupación con orden explícito.
        List<CiudadAlmacenesDto> porCiudad = await almacenes
            .GroupBy(almacen => almacen.Ciudad)
            .Select(grupo => new CiudadAlmacenesDto
            {
                Ciudad = grupo.Key,
                TotalAlmacenes = grupo.Count(),
                Activos = grupo.Count(almacen => almacen.Activo)
            })
            .OrderByDescending(ciudad => ciudad.TotalAlmacenes)
            .ThenBy(ciudad => ciudad.Ciudad)
            .ToListAsync();

        return new ResumenAlmacenesDto
        {
            TotalAlmacenes = totalAlmacenes,
            Activos = activos,
            Inactivos = totalAlmacenes - activos,
            CapacidadTotalM3 = capacidadTotalM3,
            CapacidadActivaM3 = capacidadActivaM3,
            PorTipo = porTipo,
            PorCiudad = porCiudad
        };
    }
}
