using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Queries;

public class ObtenerVehiculoFlotaQuery : IRequest<VehiculoFlotaDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerVehiculoFlotaQueryHandler : IRequestHandler<ObtenerVehiculoFlotaQuery, VehiculoFlotaDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerVehiculoFlotaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VehiculoFlotaDetalleDto> Handle(ObtenerVehiculoFlotaQuery request, CancellationToken cancellationToken)
    {
        // PATRÓN LINQ 3: proyección directa a DTO
        VehiculoFlotaDetalleDto? vehiculo = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .Where(v => v.Id == request.Id)
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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El vehículo {request.Id} no existe.");
        }

        return vehiculo;
    }
}
