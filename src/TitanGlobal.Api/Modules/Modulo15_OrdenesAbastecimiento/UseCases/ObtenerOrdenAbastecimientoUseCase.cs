using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

public class ObtenerOrdenAbastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerOrdenAbastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoDetalleDto> ExecuteAsync(int id)
    {
        DateTime hoyUtc = DateTime.UtcNow.Date;
        OrdenAbastecimientoDetalleDto? orden = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            // PATRÓN LINQ 10: proyección jerárquica de la orden y sus líneas.
            .Select(ProyeccionOrdenAbastecimiento.CrearSelector(hoyUtc))
            .SingleOrDefaultAsync();
        if (orden is null)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                $"La orden de abastecimiento {id} no existe.");
        }

        return orden;
    }
}
