using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.UseCases;

public class ObtenerZonaAlmacenajeUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerZonaAlmacenajeUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ZonaDetalleDto> ExecuteAsync(int id)
    {
        ZonaDetalleDto? zona = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new ZonaDetalleDto
            {
                Id = entidad.Id,
                AlmacenId = entidad.AlmacenId,
                AlmacenCodigo = entidad.Almacen.Codigo,
                AlmacenNombre = entidad.Almacen.Nombre,
                Codigo = entidad.Codigo,
                Nombre = entidad.Nombre,
                Tipo = entidad.Tipo,
                Temperatura = entidad.Temperatura,
                CapacidadM3 = entidad.CapacidadM3,
                Activa = entidad.Activa
            })
            .SingleOrDefaultAsync();
        if (zona is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"La zona {id} no existe.");
        }

        return zona;
    }
}
