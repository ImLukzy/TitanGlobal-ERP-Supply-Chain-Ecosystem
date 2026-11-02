using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota;

public class ResumenVehiculosFlotaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenVehiculosFlotaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenVehiculosFlotaDto> ExecuteAsync()
    {
        IQueryable<VehiculoFlota> vehiculos = _unitOfWork.Repository<VehiculoFlota>().AsQueryable();
        int totalVehiculos = await vehiculos.CountAsync();
        int activos = await vehiculos.CountAsync(vehiculo => vehiculo.Activo);
        int inactivos = totalVehiculos - activos;

        // PATRÓN LINQ 4: suma
        decimal capacidadTotalKg = await vehiculos.SumAsync(vehiculo => vehiculo.CapacidadKg);

        // PATRÓN LINQ 9: agrupación y orden por conteo
        List<TipoVehiculoDto> porTipo = await vehiculos
            .GroupBy(vehiculo => vehiculo.Tipo)
            .Select(grupo => new TipoVehiculoDto
            {
                Tipo = grupo.Key,
                TotalVehiculos = grupo.Count(),
                Activos = grupo.Count(vehiculo => vehiculo.Activo)
            })
            .OrderByDescending(dto => dto.TotalVehiculos)
            .ThenBy(dto => dto.Tipo)
            .ToListAsync();

        return new ResumenVehiculosFlotaDto
        {
            TotalVehiculos = totalVehiculos,
            Activos = activos,
            Inactivos = inactivos,
            CapacidadTotalKg = capacidadTotalKg,
            PorTipo = porTipo
        };
    }
}
