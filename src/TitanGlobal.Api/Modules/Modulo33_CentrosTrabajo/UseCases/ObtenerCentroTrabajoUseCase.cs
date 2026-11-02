using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.UseCases;

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
