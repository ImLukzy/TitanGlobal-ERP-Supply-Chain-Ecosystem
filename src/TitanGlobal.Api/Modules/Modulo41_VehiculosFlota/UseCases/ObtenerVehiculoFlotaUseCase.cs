using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

public class ObtenerVehiculoFlotaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerVehiculoFlotaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VehiculoFlotaDetalleDto> ExecuteAsync(int id)
    {
        // PATRÓN LINQ 3: proyección directa a DTO
        VehiculoFlotaDetalleDto? vehiculo = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .Where(v => v.Id == id)
            .Select(v => new VehiculoFlotaDetalleDto
            {
                Id = v.Id,
                Placa = v.Placa,
                Marca = v.Marca,
                Modelo = v.Modelo,
                Anio = v.Anio,
                Tipo = v.Tipo,
                CapacidadKg = v.CapacidadKg,
                Activo = v.Activo,
                Observaciones = v.Observaciones
            })
            .FirstOrDefaultAsync();

        if (vehiculo is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El vehículo {id} no existe.");
        }

        return vehiculo;
    }
}
