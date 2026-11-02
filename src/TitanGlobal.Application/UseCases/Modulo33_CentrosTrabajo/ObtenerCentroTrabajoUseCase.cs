using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo;

public class ObtenerCentroTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCentroTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CentroTrabajoDetalleDto> ExecuteAsync(int id)
    {
        CentroTrabajoDetalleDto? centro = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new CentroTrabajoDetalleDto
            {
                Id = entidad.Id,
                Codigo = entidad.Codigo,
                Nombre = entidad.Nombre,
                Tipo = entidad.Tipo,
                Planta = entidad.Planta,
                CapacidadHorasDia = entidad.CapacidadHorasDia,
                CostoHora = entidad.CostoHora,
                Responsable = entidad.Responsable,
                Activo = entidad.Activo
            })
            .SingleOrDefaultAsync();
        if (centro is null)
        {
            throw PersistenciaCentroTrabajo.CrearNoEncontrado(id);
        }

        return centro;
    }
}
