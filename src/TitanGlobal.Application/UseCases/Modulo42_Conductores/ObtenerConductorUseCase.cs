using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores;

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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El conductor {id} no existe.");
        }

        return conductor;
    }
}
