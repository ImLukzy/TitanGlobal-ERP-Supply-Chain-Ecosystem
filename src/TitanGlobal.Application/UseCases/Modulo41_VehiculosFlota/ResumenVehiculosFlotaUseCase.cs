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
        List<VehiculoFlota> todos = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .ToListAsync();

        int totalVehiculos = todos.Count;
        int activos = todos.Count(v => v.Activo);
        int inactivos = totalVehiculos - activos;

        // PATRÓN LINQ 4: suma
        decimal capacidadTotalKg = todos.Sum(v => v.CapacidadKg);

        // PATRÓN LINQ 9: agrupación y orden por conteo
        List<TipoVehiculoDto> porTipo = todos
            .GroupBy(v => v.Tipo)
            .Select(grupo => new TipoVehiculoDto
            {
                Tipo = grupo.Key,
                TotalVehiculos = grupo.Count(),
                Activos = grupo.Count(v => v.Activo)
            })
            .OrderByDescending(dto => dto.TotalVehiculos)
            .ThenBy(dto => dto.Tipo)
            .ToList();

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
