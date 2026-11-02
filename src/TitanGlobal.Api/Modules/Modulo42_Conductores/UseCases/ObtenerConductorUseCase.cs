using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo42_Conductores.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo42_Conductores.UseCases;

public class ObtenerConductorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerConductorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConductorDetalleDto> ExecuteAsync(int id)
    {
        ConductorDetalleDto? conductor = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .Where(conductor => conductor.Id == id)
            .Select(conductor => new ConductorDetalleDto
            {
                Id = conductor.Id,
                Nombre = conductor.Nombre,
                DocumentoLicencia = conductor.DocumentoLicencia,
                VencimientoLicencia = conductor.VencimientoLicencia,
                Habilitado = conductor.Habilitado,
                Activo = conductor.Activo,
                Observaciones = conductor.Observaciones
            })
            .FirstOrDefaultAsync();

        if (conductor is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El conductor {id} no existe.");
        }

        return conductor;
    }
}
